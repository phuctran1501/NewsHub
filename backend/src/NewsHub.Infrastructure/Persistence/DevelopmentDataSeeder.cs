namespace NewsHub.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using NewsHub.Domain.ValueObjects;
using Pgvector;
using System.Text.RegularExpressions;

/// <summary>
/// Seeder dữ liệu mẫu chuẩn báo chí tiếng Việt cho môi trường Development (100% Idempotent).
/// </summary>
public static class DevelopmentDataSeeder
{
    public static async Task SeedAsync(
        IServiceProvider services,
        bool regenerateEmbeddings = false,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NewsHubDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var aiService = scope.ServiceProvider.GetService<IAiService>();
        var cacheService = scope.ServiceProvider.GetService<ICacheService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DevelopmentDataSeeder");

        logger.LogInformation("Đang kiểm tra và áp dụng các EF Core migrations còn thiếu...");
        await dbContext.Database.MigrateAsync(cancellationToken);

        logger.LogInformation("Bắt đầu seed dữ liệu mẫu NewsHub (Idempotent)...");

        // 1. Seed 3 tài khoản Nhà báo mẫu
        var journalistSpecs = new[]
        {
            (Email: "minh.anh@newshub.vn", Name: "Nguyễn Minh Anh", Card: "NB-2026-1001", Phone: "+84901234001", Bio: "Phóng viên chuyên sâu mảng Công nghệ & Trí tuệ nhân tạo tại NewsHub."),
            (Email: "quoc.bao@newshub.vn", Name: "Trần Quốc Bảo", Card: "NB-2026-1002", Phone: "+84901234002", Bio: "Biên tập viên mảng Kinh tế số, tài chính và chuỗi cung ứng bán dẫn."),
            (Email: "thu.ha@newshub.vn", Name: "Lê Thu Hà", Card: "NB-2026-1003", Phone: "+84901234003", Bio: "Phóng viên Thời sự, đô thị thông minh và quan hệ quốc tế."),
        };

        var journalists = new List<User>();
        var defaultHash = passwordHasher.HashPassword("NewsHub@2026");

        foreach (var spec in journalistSpecs)
        {
            var emailVo = new Email(spec.Email);
            var existing = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == emailVo, cancellationToken);
            if (existing is not null)
            {
                journalists.Add(existing);
                continue;
            }

            var user = new User
            {
                Email = emailVo,
                PasswordHash = defaultHash,
                FullName = spec.Name,
                PressCardNumber = spec.Card,
                PhoneNumber = spec.Phone,
                Bio = spec.Bio,
                Role = UserRole.Journalist,
                IsActive = true,
            };
            dbContext.Users.Add(user);
            journalists.Add(user);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        // 2. Seed 5 Chuyên mục chuẩn
        var categorySpecs = new[]
        {
            (Name: "Thời sự", Slug: "thoi-su", Desc: "Tin tức thời sự trong nước, chính sách công, hạ tầng và chuyển đổi đô thị."),
            (Name: "Kinh tế", Slug: "kinh-te", Desc: "Phân tích kinh tế vĩ mô, thị trường vốn, đầu tư FDI và chuyển đổi kinh tế số."),
            (Name: "Công nghệ", Slug: "cong-nghe", Desc: "Trí tuệ nhân tạo, kiến trúc phần mềm, hạ tầng đám mây và an ninh dữ liệu."),
            (Name: "Thế giới", Slug: "the-gioi", Desc: "Diễn biến địa chính trị, kinh tế toàn cầu và chính sách công nghệ quốc tế."),
            (Name: "Đời sống", Slug: "doi-song", Desc: "Văn hóa đọc, giáo dục số, năng lượng xanh và chất lượng sống hiện đại."),
        };

        var categoryBySlug = new Dictionary<string, Category>(StringComparer.OrdinalIgnoreCase);
        foreach (var spec in categorySpecs)
        {
            var existing = await dbContext.Categories.FirstOrDefaultAsync(c => c.Slug == spec.Slug, cancellationToken);
            if (existing is not null)
            {
                categoryBySlug[spec.Slug] = existing;
                continue;
            }

            var cat = new Category
            {
                Name = spec.Name,
                Slug = spec.Slug,
                Description = spec.Desc,
            };
            dbContext.Categories.Add(cat);
            categoryBySlug[spec.Slug] = cat;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        // 3. Seed 3 Tòa soạn đã phê duyệt (Approved)
        var companySpecs = new[]
        {
            (Name: "Thời Báo Số Việt Nam", Code: "84920174", TaxCode: "0108492017", OwnerIdx: 0),
            (Name: "Kinh Tế & Dữ Liệu", Code: "39104822", TaxCode: "0303910482", OwnerIdx: 1),
            (Name: "Diễn Đàn Công Nghệ Mới", Code: "61749035", TaxCode: "0106174903", OwnerIdx: 2),
        };

        var companies = new List<Company>();
        foreach (var spec in companySpecs)
        {
            var existing = await dbContext.Companies.FirstOrDefaultAsync(c => c.Code == spec.Code, cancellationToken);
            if (existing is not null)
            {
                companies.Add(existing);
                continue;
            }

            var owner = journalists[spec.OwnerIdx];
            var company = new Company
            {
                Name = spec.Name,
                Code = spec.Code,
                TaxCode = spec.TaxCode,
                Status = CompanyStatus.Approved,
                OwnerId = owner.Id,
            };
            dbContext.Companies.Add(company);
            await dbContext.SaveChangesAsync(cancellationToken);

            dbContext.CompanyMembers.Add(new CompanyMember
            {
                CompanyId = company.Id,
                UserId = owner.Id,
                Role = CompanyMemberRole.Owner,
            });
            await dbContext.SaveChangesAsync(cancellationToken);

            companies.Add(company);
        }

        // 4. Seed 12 Bài viết đã xuất bản kèm ảnh bìa 16:9
        var now = DateTimeOffset.UtcNow;
        var articleSpecs = new[]
        {
            (
                Slug: "kien-truc-tim-kiem-ngu-nghia-pgvector-cho-toa-soan-so",
                Title: "Kiến trúc tìm kiếm ngữ nghĩa pgvector tái định hình cách độc giả tra cứu kho báo chí",
                CategorySlug: "cong-nghe",
                AuthorIdx: 0,
                CompanyIdx: (int?)0,
                ReadMin: 5,
                Views: 2480L,
                HoursAgo: 1,
                CoverUrl: "https://images.unsplash.com/photo-1518770660439-4636190af475?auto=format&fit=crop&w=1280&q=80",
                Body: "<p>Thay vì phụ thuộc hoàn toàn vào từ khóa khớp chính xác, mô hình nhúng vector 768 chiều kết hợp chỉ mục HNSW trên PostgreSQL cho phép độc giả đặt câu hỏi tự nhiên và nhận ngay các bài phóng sự phân tích sát ngữ cảnh nhất.</p><p>Các tòa soạn số hiện đại đang tách biệt tác vụ sinh vector sang hàng đợi nền Hangfire để đảm bảo luồng xuất bản chính luôn duy trì độ trễ dưới 50ms.</p>"
            ),
            (
                Slug: "thi-truong-ban-dan-dong-nam-a-ghi-nhan-von-dau-tu-ky-luc",
                Title: "Thị trường bán dẫn Đông Nam Á ghi nhận làn sóng đầu tư mở rộng chuỗi cung ứng mới",
                CategorySlug: "kinh-te",
                AuthorIdx: 1,
                CompanyIdx: (int?)1,
                ReadMin: 4,
                Views: 1940L,
                HoursAgo: 3,
                CoverUrl: "https://images.unsplash.com/photo-1551288049-bebda4e38f71?auto=format&fit=crop&w=1280&q=80",
                Body: "<p>Các trung tâm đóng gói kiểm thử vi mạch và phòng thí nghiệm thiết kế chip tại Việt Nam, Malaysia và Singapore liên tiếp công bố kế hoạch mở rộng công suất giai đoạn 2026–2028.</p><p>Chuyên gia nhận định nhân lực kỹ thuật cao và hạ tầng năng lượng sạch là hai yếu tố quyết định sức hút vốn FDI công nghệ cao.</p>"
            ),
            (
                Slug: "chuan-hoa-quy-trinh-kiem-duyet-da-tang-tai-toa-soan-hoi-tu",
                Title: "Chuẩn hóa quy trình kiểm duyệt đa tầng và minh bạch tác quyền tại tòa soạn điện tử",
                CategorySlug: "thoi-su",
                AuthorIdx: 2,
                CompanyIdx: (int?)0,
                ReadMin: 3,
                Views: 1620L,
                HoursAgo: 5,
                CoverUrl: "https://images.unsplash.com/photo-1504711434969-e33886168f5c?auto=format&fit=crop&w=1280&q=80",
                Body: "<p>Mô hình tòa soạn hội tụ cho phép phóng viên vừa tác nghiệp độc lập vừa cộng tác với nhiều cơ quan báo chí thông qua mã định danh 8 chữ số và quy trình tiền kiểm rõ ràng.</p><p>Mỗi bản chỉnh sửa đều được lưu vết giúp tăng cường niềm tin của độc giả đối với báo chí chính thống.</p>"
            ),
            (
                Slug: "lien-minh-chau-au-cap-nhat-khung-minh-bach-tin-tuc-ai",
                Title: "Liên minh châu Âu cập nhật khung minh bạch cho các hệ thống tóm tắt tin tức bằng AI",
                CategorySlug: "the-gioi",
                AuthorIdx: 2,
                CompanyIdx: (int?)2,
                ReadMin: 6,
                Views: 1380L,
                HoursAgo: 8,
                CoverUrl: "https://images.unsplash.com/photo-1529107386315-e1a2ed48a620?auto=format&fit=crop&w=1280&q=80",
                Body: "<p>Quy định mới yêu cầu mọi nền tảng tin tức phân tách rạch ròi giữa nội dung phóng viên biên soạn và phần tóm tắt, phân tích sắc thái do trí tuệ nhân tạo hỗ trợ.</p><p>Các cơ quan báo chí châu Âu đánh giá đây là bước đi cần thiết nhằm bảo vệ uy tín nguồn tin gốc.</p>"
            ),
            (
                Slug: "hang-doi-tac-vu-nen-giup-tom-tat-ai-khong-gay-nghen-xuat-ban",
                Title: "Ứng dụng hàng đợi tác vụ nền giúp tóm tắt AI tức thời mà không làm chậm xuất bản",
                CategorySlug: "cong-nghe",
                AuthorIdx: 0,
                CompanyIdx: (int?)2,
                ReadMin: 5,
                Views: 1210L,
                HoursAgo: 12,
                CoverUrl: "https://images.unsplash.com/photo-1558494949-ef010cbdcc31?auto=format&fit=crop&w=1280&q=80",
                Body: "<p>Kiến trúc bất đồng bộ kết hợp Hangfire và SignalR cho phép bài viết xuất bản lên trang chủ ngay lập tức trong khi các tác vụ gọi mô hình ngôn ngữ lớn chạy ngầm và đẩy thông báo khi hoàn tất.</p>"
            ),
            (
                Slug: "kinh-te-so-viet-nam-but-pha-nho-thanh-toan-khong-tien-mat",
                Title: "Kinh tế số Việt Nam bứt phá mạnh mẽ nhờ hạ tầng thanh toán và thương mại dữ liệu",
                CategorySlug: "kinh-te",
                AuthorIdx: 1,
                CompanyIdx: (int?)1,
                ReadMin: 4,
                Views: 1090L,
                HoursAgo: 16,
                CoverUrl: "https://images.unsplash.com/photo-1460925895917-afdab827c52f?auto=format&fit=crop&w=1280&q=80",
                Body: "<p>Tỷ trọng kinh tế số trong GDP tiếp tục mở rộng khi doanh nghiệp vừa và nhỏ đồng loạt chuyển dịch lên nền tảng quản trị đám mây và thanh toán thời gian thực.</p>"
            ),
            (
                Slug: "ha-tang-giao-thong-xanh-do-thi-buoc-chuyen-tu-quy-hoach",
                Title: "Hạ tầng giao thông xanh đô thị: Bước chuyển từ quy hoạch sang vận hành thực tế",
                CategorySlug: "thoi-su",
                AuthorIdx: 2,
                CompanyIdx: (int?)0,
                ReadMin: 4,
                Views: 980L,
                HoursAgo: 20,
                CoverUrl: "https://images.unsplash.com/photo-1477959858617-67f85cf4f1df?auto=format&fit=crop&w=1280&q=80",
                Body: "<p>Các tuyến kết nối xe buýt điện, đường sắt đô thị và bãi đỗ thông minh đang rút ngắn đáng kể thời gian di chuyển giờ cao điểm tại hai đô thị lớn nhất cả nước.</p>"
            ),
            (
                Slug: "bao-mat-xac-thuc-phien-jwt-bo-nho-va-httponly-cookie",
                Title: "Bảo mật xác thực phiên đăng nhập báo điện tử bằng JWT trong RAM và HttpOnly Cookie",
                CategorySlug: "cong-nghe",
                AuthorIdx: 0,
                CompanyIdx: (int?)2,
                ReadMin: 6,
                Views: 870L,
                HoursAgo: 26,
                CoverUrl: "https://images.unsplash.com/photo-1555949963-ff9fe0c870eb?auto=format&fit=crop&w=1280&q=80",
                Body: "<p>Lưu trữ Access Token trong bộ nhớ ứng dụng kết hợp Refresh Token Rotation trên HttpOnly Cookie giúp loại bỏ hoàn toàn nguy cơ đánh cắp phiên qua lỗ hổng XSS từ LocalStorage.</p>"
            ),
            (
                Slug: "chuyen-dich-nang-luong-xanh-trong-khu-cong-nghiep-phia-nam",
                Title: "Chuyển dịch năng lượng xanh tại các khu công nghiệp hướng tới tiêu chuẩn xuất khẩu mới",
                CategorySlug: "kinh-te",
                AuthorIdx: 1,
                CompanyIdx: (int?)1,
                ReadMin: 5,
                Views: 760L,
                HoursAgo: 32,
                CoverUrl: "https://images.unsplash.com/photo-1509391366360-2e959784a276?auto=format&fit=crop&w=1280&q=80",
                Body: "<p>Điện mặt trời áp mái và cơ chế mua bán điện trực tiếp giúp doanh nghiệp sản xuất giảm mạnh phát thải carbon để đáp ứng cơ chế điều chỉnh biên giới carbon của châu Âu.</p>"
            ),
            (
                Slug: "chuan-hoa-anh-webp-tu-dong-toi-uu-toc-do-doc-bao-di-dong",
                Title: "Chuẩn hóa định dạng ảnh WebP qua CDN giúp tăng gấp đôi tốc độ đọc báo trên di động",
                CategorySlug: "cong-nghe",
                AuthorIdx: 0,
                CompanyIdx: null,
                ReadMin: 3,
                Views: 640L,
                HoursAgo: 40,
                CoverUrl: "https://images.unsplash.com/photo-1526374965328-7f61d4dc18c5?auto=format&fit=crop&w=1280&q=80",
                Body: "<p>Tự động chuyển đổi hình ảnh sang định dạng WebP và tải lười (lazy-loading) giúp giảm tới 60% băng thông di động mà vẫn bảo toàn độ sắc nét cho ảnh phóng sự.</p>"
            ),
            (
                Slug: "hop-tac-du-lieu-mo-khu-vuc-chau-a-thai-binh-duong",
                Title: "Hợp tác dữ liệu mở và chia sẻ cảnh báo khí hậu tại khu vực châu Á - Thái Bình Dương",
                CategorySlug: "the-gioi",
                AuthorIdx: 2,
                CompanyIdx: (int?)0,
                ReadMin: 5,
                Views: 590L,
                HoursAgo: 48,
                CoverUrl: "https://images.unsplash.com/photo-1451187580459-43490279c0fa?auto=format&fit=crop&w=1280&q=80",
                Body: "<p>Các viện khí tượng trong khu vực thống nhất chuẩn chia sẻ dữ liệu vệ tinh thời gian thực nhằm nâng cao năng lực dự báo sớm thiên tai và bảo vệ cộng đồng ven biển.</p>"
            ),
            (
                Slug: "van-hoa-doc-sau-trong-ky-nguyen-tin-tuc-tuc-thoi",
                Title: "Giữ gìn văn hóa đọc sâu trong kỷ nguyên tin tức tức thời và mạng xã hội",
                CategorySlug: "doi-song",
                AuthorIdx: 1,
                CompanyIdx: null,
                ReadMin: 4,
                Views: 520L,
                HoursAgo: 56,
                CoverUrl: "https://images.unsplash.com/photo-1456513080510-7bf3a84b82f8?auto=format&fit=crop&w=1280&q=80",
                Body: "<p>Nghệ thuật trình bày chữ viết thoáng đãng, loại bỏ các yếu tố gây xao nhãng và tóm tắt ý chính thông minh đang giúp độc giả quay lại với những bài phân tích dài chất lượng cao.</p>"
            ),
        };

        var insertedCount = 0;
        foreach (var spec in articleSpecs)
        {
            var exists = await dbContext.Articles.AnyAsync(a => a.Slug == spec.Slug, cancellationToken);
            if (exists) continue;

            var author = journalists[spec.AuthorIdx];
            var mediaPublicId = $"seed/covers/{spec.Slug}";
            var media = await dbContext.MediaFiles.FirstOrDefaultAsync(m => m.PublicId == mediaPublicId, cancellationToken);
            if (media is null)
            {
                media = new MediaFile
                {
                    PublicId = mediaPublicId,
                    Url = spec.CoverUrl,
                    OriginalFileName = $"{spec.Slug}.webp",
                    Format = "webp",
                    Bytes = 148_000,
                    Width = 1280,
                    Height = 720,
                    Folder = MediaFolder.ArticleCover,
                    UploadedByUserId = author.Id,
                };
                dbContext.MediaFiles.Add(media);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            var publishedAt = now.AddHours(-spec.HoursAgo);
            var article = new Article
            {
                AuthorId = author.Id,
                CompanyId = spec.CompanyIdx.HasValue ? companies[spec.CompanyIdx.Value].Id : null,
                Title = spec.Title,
                Slug = spec.Slug,
                ContentHtml = spec.Body,
                Status = ArticleStatus.Published,
                CoverImageId = media.Id,
                ReadingTimeMinutes = spec.ReadMin,
                ViewCount = spec.Views,
                PublishedAt = publishedAt,
                CreatedAt = publishedAt,
            };

            dbContext.Articles.Add(article);
            await dbContext.SaveChangesAsync(cancellationToken);

            if (categoryBySlug.TryGetValue(spec.CategorySlug, out var cat))
            {
                dbContext.ArticleCategories.Add(new ArticleCategory
                {
                    ArticleId = article.Id,
                    CategoryId = cat.Id,
                });
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            insertedCount++;
        }

        // 5. Sinh vector nhúng thật (pgvector 768 chiều, RETRIEVAL_DOCUMENT + L2-normalized) cho các bài đã xuất bản
        var embeddedCount = 0;
        if (aiService is not null)
        {
            var publishedArticles = await dbContext.Articles
                .Include(a => a.ArticleCategories)
                    .ThenInclude(ac => ac.Category)
                .Include(a => a.ArticleEmbedding)
                .Where(a => a.Status == ArticleStatus.Published)
                .ToListAsync(cancellationToken);

            foreach (var article in publishedArticles)
            {
                if (!regenerateEmbeddings && article.ArticleEmbedding is not null)
                {
                    continue;
                }

                try
                {
                    var plainText = Regex.Replace(article.ContentHtml ?? string.Empty, "<.*?>", " ");
                    plainText = Regex.Replace(plainText, @"\s+", " ").Trim();
                    var snippet = plainText.Length > 1200 ? plainText[..1200] : plainText;
                    var categoryNames = article.ArticleCategories
                        .Where(ac => ac.Category != null)
                        .Select(ac => ac.Category!.Name)
                        .Distinct()
                        .ToList();
                    var categoryPrefix = categoryNames.Count > 0
                        ? $"Chuyên mục: {string.Join(", ", categoryNames)}. "
                        : string.Empty;

                    var textToEmbed = $"{categoryPrefix}{article.Title}. {snippet}".Trim();
                    var values = await aiService.GenerateEmbeddingAsync(
                        textToEmbed,
                        EmbeddingTaskType.RetrievalDocument,
                        article.Title,
                        cancellationToken);

                    var vector = new Vector(values);
                    if (article.ArticleEmbedding is not null)
                    {
                        article.ArticleEmbedding.Embedding = vector;
                        article.ArticleEmbedding.UpdatedAt = DateTimeOffset.UtcNow;
                    }
                    else
                    {
                        dbContext.ArticleEmbeddings.Add(new ArticleEmbedding
                        {
                            Id = Guid.CreateVersion7(),
                            ArticleId = article.Id,
                            Embedding = vector,
                            UpdatedAt = DateTimeOffset.UtcNow,
                        });
                    }

                    await dbContext.SaveChangesAsync(cancellationToken);
                    embeddedCount++;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Không thể tạo embedding cho bài viết {Slug}. Tiếp tục các bài khác.", article.Slug);
                }
            }
        }

        // 6. Xóa cache Redis để Frontend nhận dữ liệu mới ngay lập tức
        if (cacheService is not null)
        {
            await cacheService.RemoveAsync("categories:all", cancellationToken);
            await cacheService.RemoveByPrefixAsync("companies:public:", cancellationToken);
            await cacheService.RemoveByPrefixAsync("ai:search:emb:", cancellationToken);
        }

        logger.LogInformation(
            "Seed hoàn tất: {Journalists} nhà báo, {Categories} chuyên mục, {Companies} tòa soạn, +{Articles} bài viết mới, +{Embeddings} vector embeddings.",
            journalists.Count,
            categoryBySlug.Count,
            companies.Count,
            insertedCount,
            embeddedCount);
    }

    /// <summary>
    /// Chạy kiểm thử nhanh Hybrid Search (pgvector + lexical) trực tiếp trên dữ liệu thật trong môi trường Development.
    /// </summary>
    public static async Task VerifyHybridSearchAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<MediatR.ISender>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("HybridSearchVerify");

        var testQueries = new[]
        {
            "hệ thống tóm tắt tin tức",
            "làm sao tránh lộ token khi bị tấn công XSS?",
            "chính sách giảm khí thải carbon nhà máy khu công nghiệp",
            "xe buýt điện đường sắt đô thị giờ cao điểm",
        };

        foreach (var q in testQueries)
        {
            var results = await mediator.Send(
                new NewsHub.Application.Features.Ai.Queries.SearchArticlesByNlp.SearchArticlesByNlpQuery(q, 5),
                cancellationToken);

            logger.LogInformation("=== QUERY: \"{Query}\" -> {Count} kết quả ===", q, results.Count);
            foreach (var r in results)
            {
                logger.LogInformation("  [{Score:P1}] {Title} (/articles/{Slug})", r.SimilarityScore, r.Title, r.Slug);
            }
        }
    }
}
