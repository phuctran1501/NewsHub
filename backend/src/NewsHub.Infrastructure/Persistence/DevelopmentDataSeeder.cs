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

        // 2. Seed 16 Chuyên mục báo chí chuẩn (tham khảo cơ cấu Tuổi Trẻ)
        var categorySpecs = new[]
        {
            (Name: "Thời sự", Slug: "thoi-su", Desc: "Tin tức thời sự trong nước, chính sách công, hạ tầng và chuyển đổi đô thị."),
            (Name: "Thế giới", Slug: "the-gioi", Desc: "Diễn biến địa chính trị, kinh tế toàn cầu và chính sách công nghệ quốc tế."),
            (Name: "Pháp luật", Slug: "phap-luat", Desc: "Pháp đình, an ninh trật tự, chính sách pháp lý và tư vấn quyền công dân."),
            (Name: "Kinh doanh", Slug: "kinh-doanh", Desc: "Doanh nghiệp, khởi nghiệp, tài chính ngân hàng, bất động sản và thị trường."),
            (Name: "Kinh tế", Slug: "kinh-te", Desc: "Phân tích kinh tế vĩ mô, thị trường vốn, đầu tư FDI và chuyển đổi kinh tế số."),
            (Name: "Công nghệ", Slug: "cong-nghe", Desc: "Trí tuệ nhân tạo, kiến trúc phần mềm, hạ tầng đám mây và an ninh dữ liệu."),
            (Name: "Khoa học", Slug: "khoa-hoc", Desc: "Phát minh mới, nghiên cứu học thuật, vũ trụ, năng lượng và môi trường."),
            (Name: "Xe", Slug: "xe", Desc: "Thị trường ô tô, xe điện, giao thông thông minh và đánh giá phương tiện."),
            (Name: "Giáo dục", Slug: "giao-duc", Desc: "Tuyển sinh, đại học, đổi mới phương pháp học tập và du học quốc tế."),
            (Name: "Sức khỏe", Slug: "suc-khoe", Desc: "Y tế dự phòng, dinh dưỡng, tiến bộ y khoa và chăm sóc sức khỏe cộng đồng."),
            (Name: "Đời sống", Slug: "doi-song", Desc: "Văn hóa đọc, gia đình, năng lượng xanh và chất lượng sống hiện đại."),
            (Name: "Du lịch", Slug: "du-lich", Desc: "Điểm đến trong nước và quốc tế, ẩm thực vùng miền và xu hướng dịch chuyển."),
            (Name: "Văn hóa", Slug: "van-hoa", Desc: "Di sản, nghệ thuật, xuất bản sách và đời sống văn hóa đương đại."),
            (Name: "Giải trí", Slug: "giai-tri", Desc: "Điện ảnh, âm nhạc, sân khấu và sự kiện giải trí nổi bật."),
            (Name: "Thể thao", Slug: "the-thao", Desc: "Bóng đá trong nước và quốc tế, điền kinh và các giải đấu đỉnh cao."),
            (Name: "Bạn đọc", Slug: "ban-doc", Desc: "Phản ánh dân sinh, đường dây nóng tòa soạn và đối thoại cùng bạn đọc."),
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
            // ── 6 Bài phóng sự chuyên sâu dài (tham khảo phong cách & chủ đề thời sự Tuổi Trẻ) ──
            (
                Slug: "metro-so-1-tphcm-thay-doi-thoi-quen-di-lai-do-thi",
                Title: "Tuyến Metro số 1 TP.HCM và cuộc chuyển dịch thói quen đi lại của hàng trăm nghìn người dân",
                CategorySlug: "thoi-su",
                AuthorIdx: 2,
                CompanyIdx: (int?)0,
                ReadMin: 7,
                Views: 4820L,
                HoursAgo: 0,
                CoverUrl: "https://images.unsplash.com/photo-1519501025264-65ba15a82390?auto=format&fit=crop&w=1280&q=80",
                Body: "<p>Sau giai đoạn vận hành thương mại ổn định, tuyến đường sắt đô thị số 1 (Bến Thành – Suối Tiên) không chỉ giải tỏa áp lực kẹt xe dọc trục cửa ngõ phía Đông TP.HCM mà còn từng bước hình thành văn hóa sử dụng giao thông công cộng văn minh, đúng giờ cho cư dân đô thị.</p><h2>Kết nối đa phương thức: Chìa khóa giữ chân hành khách</h2><p>Ghi nhận tại các nhà ga trọng điểm như Thủ Đức, Thảo Điền và Bến Thành vào khung giờ cao điểm sáng từ 6h30 đến 8h30, lượng hành khách là nhân viên văn phòng và sinh viên tăng đều qua từng tuần. Điểm thay đổi rõ rệt nhất là sự hiện diện của mạng lưới 17 tuyến xe buýt điện gom khách miễn phí và có trợ giá kết nối trực tiếp vào các khu dân cư, trường đại học và khu công nghệ cao.</p><p>Bên cạnh đó, hệ thống bãi giữ xe cá nhân quanh các ga trên cao đã được mở rộng với cơ chế thanh toán không tiền mặt qua thẻ ngân hàng, ví điện tử và mã QR liên thông. Người dân chỉ mất chưa đầy 30 phút để di chuyển từ TP. Thủ Đức vào trung tâm quận 1, rút ngắn gần một nửa thời gian so với việc tự lái xe máy qua xa lộ Hà Nội trong điều kiện ùn tắc.</p><blockquote>“Trước đây mỗi sáng tôi phải rời nhà lúc 6h15 để tránh kẹt xe ở ngã tư Thủ Đức và cầu Sài Gòn. Nay tôi gửi xe gần ga, lên tàu đọc báo khoảng 25 phút là tới ngay gần cơ quan, vừa tiết kiệm sức lực vừa chủ động hoàn toàn lịch trình.” — Chị Minh Thư, nhân viên tài chính tại quận 1 chia sẻ.</blockquote><h2>Động lực tái cấu trúc không gian đô thị theo mô hình TOD</h2><p>Theo các chuyên gia quy hoạch giao thông, giá trị lớn nhất của Metro số 1 không dừng lại ở năng lực chuyên chở hành khách mà nằm ở mô hình phát triển đô thị gắn với giao thông công cộng (TOD). Dọc hành lang tuyến, nhiều tổ hợp thương mại, văn phòng, không gian đi bộ và công viên bờ sông đang được điều chỉnh quy hoạch nhằm khuyến khích người dân đi bộ trong bán kính 500 đến 800 mét quanh nhà ga.</p><ul><li>Tăng tỷ lệ phủ sóng xe buýt điện gom khách kết nối trực tiếp các khu đô thị mới và khu công nghệ cao.</li><li>Hoàn thiện hệ thống vé liên thông điện tử giữa đường sắt đô thị, xe buýt và xe đạp công cộng.</li><li>Đẩy nhanh chuẩn bị đầu tư tuyến Metro số 2 (Bến Thành – Tham Lương) để sớm hình thành mạng lưới khép kín.</li></ul><p>Giới chuyên môn nhấn mạnh rằng thành công bước đầu của tuyến số 1 là bài học thực tiễn quý giá về quản trị vận hành, bảo trì kỹ thuật và truyền thông hướng dẫn người dân, tạo tiền đề vững chắc cho mục tiêu hoàn thiện hàng trăm kilômét đường sắt đô thị tại TP.HCM và Hà Nội trong thập kỷ tới.</p>"
            ),
            (
                Slug: "nong-san-viet-nam-vuot-rao-can-xanh-chinh-phuc-thi-truong-cao-cap",
                Title: "Nông sản Việt vượt rào cản xanh: Từ mã số vùng trồng đến chuỗi lạnh xuất khẩu tỷ USD",
                CategorySlug: "kinh-te",
                AuthorIdx: 1,
                CompanyIdx: (int?)1,
                ReadMin: 7,
                Views: 3910L,
                HoursAgo: 1,
                CoverUrl: "https://images.unsplash.com/photo-1464226184884-fa280b87c399?auto=format&fit=crop&w=1280&q=80",
                Body: "<p>Kim ngạch xuất khẩu rau quả, cà phê đặc sản và gạo chất lượng cao của Việt Nam liên tiếp lập kỷ lục mới, nhưng đằng sau những con số tăng trưởng ấn tượng là cuộc chạy đua khốc liệt nhằm đáp ứng tiêu chuẩn truy xuất nguồn gốc, dư lượng thuốc bảo vệ thực vật và giảm phát thải carbon tại các thị trường khó tính.</p><h2>Minh bạch hóa bằng dữ liệu số tại từng vườn trồng</h2><p>Tại Đồng bằng sông Cửu Long và Tây Nguyên, hàng nghìn hợp tác xã đã chuyển từ ghi chép sổ tay sang nhật ký canh tác điện tử gắn với mã số vùng trồng và cơ sở đóng gói. Mỗi lô hàng trước khi rời kho đều có mã QR chứa đầy đủ thông tin về giống cây, lịch bón phân, thời điểm thu hoạch và kết quả kiểm nghiệm an toàn thực phẩm tại phòng thí nghiệm đạt chuẩn quốc tế.</p><p>Đại diện nhiều doanh nghiệp xuất khẩu cho biết việc đầu tư hệ thống kiểm soát chất lượng ngay từ đầu giúp tỷ lệ lô hàng bị cảnh báo tại cửa khẩu giảm xuống mức thấp nhất trong nhiều năm, đồng thời nâng giá bán trung bình thêm 15% đến 25% so với hàng xuất thô truyền thống.</p><blockquote>“Thị trường châu Âu, Nhật Bản và Hàn Quốc sẵn sàng trả giá rất cao cho trái cây nhiệt đới và cà phê Việt Nam, với điều kiện doanh nghiệp chứng minh được quy trình sản xuất bền vững và chất lượng đồng đều qua từng mùa vụ.” — Ông Nguyễn Văn Thành, Giám đốc một doanh nghiệp xuất khẩu nông sản tại Tiền Giang nhận định.</blockquote><h2>Nút thắt chuỗi cung ứng lạnh và chế biến sâu</h2><p>Dù sản lượng dồi dào, chi phí logistics vẫn chiếm tỷ trọng cao trong giá thành nông sản xuất khẩu. Để giải bài toán này, nhiều tập đoàn nông nghiệp đang mạnh tay đầu tư nhà máy cấp đông nhanh bằng nitơ lỏng, kho lạnh thông minh gần vùng nguyên liệu và dây chuyền chế biến sâu như trái cây sấy thăng hoa, nước ép cô đặc và cà phê hòa tan đặc sản.</p><ul><li>Mở rộng vùng nguyên liệu đạt chứng nhận VietGAP, GlobalGAP và hữu cơ gắn với hợp đồng bao tiêu dài hạn.</li><li>Phát triển trung tâm logistics nông sản chuyên biệt kết nối trực tiếp cảng nước sâu và đường sắt liên vận.</li><li>Đa dạng hóa thị trường xuất khẩu nhằm giảm rủi ro phụ thuộc vào một khu vực đơn lẻ.</li></ul><p>Khi nông dân, hợp tác xã và doanh nghiệp cùng chia sẻ dữ liệu trên một chuỗi giá trị minh bạch, nông sản Việt Nam hoàn toàn có cơ sở chuyển dịch từ lợi thế giá rẻ sang thương hiệu chất lượng cao có sức cạnh tranh bền vững trên bản đồ thương mại toàn cầu.</p>"
            ),
            (
                Slug: "chuyen-doi-so-y-te-benh-an-dien-tu-lien-thong-toan-quoc",
                Title: "Chuyển đổi số y tế: Khi bệnh án điện tử giúp người dân không còn mang theo xấp phim chụp dày cộm",
                CategorySlug: "doi-song",
                AuthorIdx: 2,
                CompanyIdx: (int?)0,
                ReadMin: 6,
                Views: 3450L,
                HoursAgo: 2,
                CoverUrl: "https://images.unsplash.com/photo-1576091160550-2173dba999ef?auto=format&fit=crop&w=1280&q=80",
                Body: "<p>Cảnh người bệnh xếp hàng từ 4 giờ sáng với tập hồ sơ giấy, kết quả xét nghiệm và phim X-quang cồng kềnh đang dần lùi vào quá khứ tại nhiều bệnh viện tuyến trung ương và tuyến tỉnh nhờ tiến độ triển khai bệnh án điện tử và sổ sức khỏe tích hợp trên ứng dụng định danh số.</p><h2>Giảm thời gian chờ đợi, tăng độ chính xác chẩn đoán</h2><p>Tại nhiều bệnh viện đa khoa lớn ở TP.HCM, Hà Nội và Đà Nẵng, người dân có thể đặt lịch khám theo khung giờ ngay trên điện thoại, thanh toán viện phí không dùng tiền mặt và nhận kết quả xét nghiệm trực tuyến. Quan trọng hơn, bác sĩ chỉ mất vài giây để tra cứu toàn bộ lịch sử dùng thuốc, dị ứng và các đợt điều trị trước đó của bệnh nhân, hạn chế tối đa tình trạng phải chụp chiếu, xét nghiệm lặp lại gây tốn kém.</p><p>Hệ thống lưu trữ và truyền tải hình ảnh y khoa (PACS) kết hợp công cụ hỗ trợ đọc phim bằng trí tuệ nhân tạo còn giúp phát hiện sớm các tổn thương nhỏ trên ảnh chụp cắt lớp phổi và võng mạc, giảm tải áp lực đáng kể cho đội ngũ bác sĩ chẩn đoán hình ảnh ở tuyến cơ sở.</p><blockquote>“Mục tiêu cuối cùng của chuyển đổi số y tế không phải là mua thêm máy chủ hay phần mềm, mà là giúp mỗi bệnh nhân được chăm sóc nhanh hơn, an toàn hơn và minh bạch chi phí điều trị.” — PGS.TS Trần Minh Quân, chuyên gia quản trị bệnh viện chia sẻ.</blockquote><h2>Thách thức liên thông dữ liệu và bảo mật thông tin bệnh nhân</h2><p>Tuy đạt nhiều bước tiến tích cực, quá trình liên thông dữ liệu giữa các bệnh viện công lập, tư nhân và trạm y tế phường xã vẫn đối mặt với sự khác biệt về chuẩn phần mềm và hạ tầng bảo mật. Các chuyên gia công nghệ y tế kiến nghị cần thống nhất chuẩn trao đổi dữ liệu quốc tế HL7 FHIR, mã hóa đầu cuối hồ sơ sức khỏe và kiểm toán truy cập nghiêm ngặt để bảo vệ tuyệt đối quyền riêng tư của người bệnh.</p><p>Khi sổ sức khỏe điện tử thực sự đồng hành cùng mỗi công dân từ lúc mới sinh đến tuổi xế chiều, ngành y tế sẽ có bức tranh dữ liệu dịch tễ chính xác theo thời gian thực để chuyển trọng tâm từ chữa bệnh sang y tế dự phòng chủ động.</p>"
            ),
            (
                Slug: "ai-trong-giang-duong-dai-hoc-viet-nam-day-cach-dat-cau-hoi",
                Title: "Trí tuệ nhân tạo trong giảng đường đại học Việt Nam: Dạy sinh viên cách phản biện thay vì cấm đoán",
                CategorySlug: "cong-nghe",
                AuthorIdx: 0,
                CompanyIdx: (int?)2,
                ReadMin: 6,
                Views: 3120L,
                HoursAgo: 4,
                CoverUrl: "https://images.unsplash.com/photo-1523240795612-9a054b0db644?auto=format&fit=crop&w=1280&q=80",
                Body: "<p>Sự phổ biến nhanh chóng của các mô hình ngôn ngữ lớn đã khiến nhiều trường đại học trên thế giới lẫn tại Việt Nam buộc phải xem xét lại phương thức kiểm tra, đánh giá truyền thống. Thay vì ban hành lệnh cấm tuyệt đối khó thực thi, xu hướng mới là đưa AI trở thành công cụ học tập có định hướng đi kèm quy tắc liêm chính học thuật rõ ràng.</p><h2>Đổi mới cách ra đề và đánh giá năng lực thực chất</h2><p>Nhiều khoa công nghệ thông tin, kinh tế và truyền thông tại các trường đại học lớn đã chuyển tỷ trọng đánh giá từ tiểu luận viết ở nhà sang vấn đáp trực tiếp, bảo vệ đồ án nhóm, giải quyết tình huống thực tế từ doanh nghiệp và kiểm tra tư duy phản biện. Sinh viên được phép sử dụng trợ lý AI để tìm tài liệu, cấu trúc dàn ý hoặc rà soát lỗi lập trình, nhưng bắt buộc phải công khai phần hỗ trợ của AI và tự chịu trách nhiệm hoàn toàn về tính chính xác của dữ liệu trích dẫn.</p><blockquote>“Một kỹ sư hay nhà báo tương lai không thể làm việc mà thiếu AI, nhưng họ cũng sẽ nhanh chóng bị đào thải nếu không đủ kiến thức nền tảng để nhận ra khi nào AI đang trả lời sai lệch.” — TS. Phạm Lan Anh, giảng viên đại học tại TP.HCM nhấn mạnh.</blockquote><h2>Xây dựng năng lực số và đạo đức dữ liệu cho thế hệ trẻ</h2><ul><li>Tích hợp học phần kỹ năng đặt câu hỏi (prompt engineering), kiểm chứng nguồn tin và bảo vệ dữ liệu cá nhân ngay từ năm nhất.</li><li>Đầu tư hạ tầng tính toán và phòng thí nghiệm AI mở hợp tác cùng các tập đoàn công nghệ để sinh viên thực hành trên bài toán thật.</li><li>Ban hành bộ khung hướng dẫn trích dẫn và liêm chính học thuật khi sử dụng công cụ AI tạo sinh trong nghiên cứu khoa học.</li></ul><p>Các chuyên gia giáo dục đánh giá đây là thời điểm bản lề giúp giáo dục đại học Việt Nam thu hẹp khoảng cách với khu vực, chuyển từ truyền thụ kiến thức ghi nhớ sang đào tạo năng lực giải quyết vấn đề phức tạp và sáng tạo liên ngành.</p>"
            ),
            (
                Slug: "nha-o-xa-hoi-do-thi-go-nut-that-quy-dat-va-tin-dung-uu-dai",
                Title: "Gỡ nút thắt nhà ở xã hội tại các đô thị lớn: Bài toán quỹ đất sạch và dòng vốn dài hạn",
                CategorySlug: "kinh-te",
                AuthorIdx: 1,
                CompanyIdx: (int?)1,
                ReadMin: 6,
                Views: 2780L,
                HoursAgo: 6,
                CoverUrl: "https://images.unsplash.com/photo-1486406146926-c627a92ad1ab?auto=format&fit=crop&w=1280&q=80",
                Body: "<p>Nhu cầu nhà ở vừa túi tiền của công nhân khu công nghiệp, người lao động trẻ và viên chức tại các đô thị lớn vẫn luôn ở mức rất cao, nhưng nguồn cung nhà ở xã hội trong những năm qua chưa theo kịp tốc độ đô thị hóa. Các chính sách sửa đổi mới đang tập trung tháo gỡ đồng bộ thủ tục đầu tư, quỹ đất và cơ chế tín dụng ưu đãi.</p><h2>Rút ngắn thời gian phê duyệt dự án và minh bạch đối tượng thụ hưởng</h2><p>Một trong những rào cản lớn nhất trước đây là quy trình chấp thuận chủ trương đầu tư, thẩm định giá bán và xác nhận điều kiện thu nhập của người mua kéo dài nhiều tháng. Với cơ chế mới liên thông dữ liệu dân cư và cơ quan thuế, việc xác minh điều kiện thu nhập và tình trạng nhà ở được rút gọn đáng kể, giảm thiểu tiêu cực và tình trạng đầu cơ suất mua.</p><p>Đồng thời, nhiều địa phương đã chủ động quy hoạch các khu nhà ở xã hội quy mô lớn gắn với hạ tầng trường học, trạm y tế và tuyến giao thông công cộng thay vì bố trí rời rạc ở những khu vực thiếu tiện ích cơ bản.</p><blockquote>“Người lao động trẻ cần những gói vay mua nhà có lãi suất ổn định trong 15 đến 20 năm để yên tâm lập nghiệp, thay vì chỉ ưu đãi trong 1–2 năm đầu rồi thả nổi theo thị trường.” — Chuyên gia kinh tế bất động sản Lê Hoàng Nam phân tích.</blockquote><h2>Phát triển mô hình nhà ở cho thuê dài hạn</h2><p>Bên cạnh phân khúc bán đứt, các nhà hoạch định chính sách đang đẩy mạnh cơ chế phát triển quỹ nhà ở xã hội cho thuê với giá hợp lý thông qua Quỹ nhà ở quốc gia và sự tham gia của doanh nghiệp bất động sản có năng lực. Cách tiếp cận linh hoạt này giúp người lao động mới ra trường dễ dàng tiếp cận chỗ ở an toàn mà không phải gánh áp lực nợ vay quá lớn ngay từ đầu.</p>"
            ),
            (
                Slug: "chuoi-cung-ung-toan-cau-dich-chuyen-co-hoi-cho-cang-bien-nuoc-sau",
                Title: "Chuỗi cung ứng toàn cầu tái định hình: Cơ hội bứt phá cho hệ thống cảng biển nước sâu châu Á",
                CategorySlug: "the-gioi",
                AuthorIdx: 2,
                CompanyIdx: (int?)0,
                ReadMin: 6,
                Views: 2390L,
                HoursAgo: 9,
                CoverUrl: "https://images.unsplash.com/photo-1494412574643-ff11b0a5c1c3?auto=format&fit=crop&w=1280&q=80",
                Body: "<p>Trước những biến động địa chính trị trên các tuyến hàng hải trọng yếu và chiến lược đa dạng hóa nguồn cung của các tập đoàn đa quốc gia, khu vực Đông Nam Á đang nổi lên như mắt xích trung chuyển chiến lược trong mạng lưới thương mại biển toàn cầu.</p><h2>Tự động hóa cảng biển và tiêu chuẩn tàu xanh</h2><p>Các cụm cảng nước sâu tại khu vực châu Á – Thái Bình Dương đang chạy đua nâng cấp cần cẩu tự động, hệ thống quản lý bãi container bằng AI và hạ tầng cấp điện bờ nhằm giảm phát thải khí nhà kính khi tàu neo đậu. Việc tiếp nhận trực tiếp các siêu tàu container trọng tải trên 200.000 tấn giúp hàng hóa xuất khẩu đi thẳng tới bờ Đông, bờ Tây nước Mỹ và châu Âu mà không cần trung chuyển qua cảng nước thứ ba, tiết kiệm từ 3 đến 5 ngày vận chuyển.</p><blockquote>“Cạnh tranh cảng biển trong thập kỷ mới không chỉ đo bằng chiều sâu luồng tàu, mà còn bằng tốc độ thông quan số hóa và khả năng kết nối mượt mà với đường bộ cao tốc, đường thủy nội địa và đường sắt.” — Báo cáo logistics hàng hải khu vực nhận định.</blockquote><h2>Liên kết vùng để tối ưu chi phí logistics</h2><p>Tại Việt Nam, việc đồng bộ hóa cụm cảng Cái Mép – Thị Vải, Lạch Huyện cùng dự án cảng trung chuyển quốc tế mới kết hợp mạng lưới cao tốc liên vùng đang mở ra dư địa tăng trưởng mạnh mẽ cho xuất nhập khẩu. Các chuyên gia nhấn mạnh rằng đầu tư song hành cho trung tâm dịch vụ hậu cần sau cảng và hải quan thông minh sẽ giúp hạ chi phí logistics quốc gia xuống mức cạnh tranh hàng đầu khu vực ASEAN.</p>"
            ),
            (
                Slug: "siet-chat-xu-ly-lua-dao-cong-nghe-cao-va-bao-ve-du-lieu-ca-nhan",
                Title: "Siết chặt xử lý lừa đảo công nghệ cao: Cơ chế khóa nhanh tài khoản nghi vấn và bảo vệ dữ liệu cá nhân",
                CategorySlug: "phap-luat",
                AuthorIdx: 2,
                CompanyIdx: (int?)0,
                ReadMin: 5,
                Views: 3120L,
                HoursAgo: 3,
                CoverUrl: "https://images.unsplash.com/photo-1589829545856-d10d557cf95f?auto=format&fit=crop&w=1280&q=80",
                Body: "<p>Trước tình trạng các đường dây lừa đảo trực tuyến sử dụng giả mạo giọng nói (deepfake), ứng dụng chứa mã độc và tài khoản ngân hàng thuê mượn ngày càng tinh vi, cơ quan lập pháp và ngành ngân hàng đang triển khai cơ chế phối hợp phong tỏa dòng tiền theo thời gian thực.</p><h2>Truy vết dòng tiền trong khung giờ vàng</h2><p>Quy trình liên thông mới giữa cơ quan điều tra, hệ thống thanh toán quốc gia và các ngân hàng thương mại cho phép gắn cờ cảnh báo tức thì đối với những tài khoản có dấu hiệu nhận tiền bất thường từ nhiều nạn nhân. Nhờ xác thực sinh trắc học bắt buộc đối với giao dịch giá trị lớn, số vụ chiếm đoạt tài khoản qua tài khoản không chính chủ đã giảm rõ rệt trong các quý gần đây.</p><blockquote>“Bảo vệ công dân trên không gian mạng đòi hỏi cả hành lang pháp lý đủ sức răn đe đối với hành vi mua bán dữ liệu cá nhân lẫn cơ chế ứng cứu tài chính nhanh chỉ tính bằng phút.” — Luật sư Trần Đình Khoa nhận định.</blockquote><h2>Khuyến nghị phòng tránh rủi ro cho người dân</h2><ul><li>Tuyệt đối không cài đặt tệp tin ngoài kho ứng dụng chính thức hoặc cung cấp mã OTP cho bất kỳ ai tự xưng cơ quan chức năng.</li><li>Kích hoạt xác thực hai lớp và giới hạn hạn mức chuyển khoản phù hợp nhu cầu sử dụng hằng ngày.</li><li>Trình báo ngay đường dây nóng công an và ngân hàng trong vòng 30 phút đầu nếu phát hiện dấu hiệu bị lừa đảo.</li></ul>"
            ),
            (
                Slug: "doanh-nghiep-xuat-khau-viet-nam-chuyen-xanh-de-vuot-rao-can-cbam",
                Title: "Doanh nghiệp xuất khẩu Việt Nam tăng tốc chuyển đổi xanh để vượt hàng rào thuế carbon châu Âu",
                CategorySlug: "kinh-doanh",
                AuthorIdx: 1,
                CompanyIdx: (int?)1,
                ReadMin: 6,
                Views: 2640L,
                HoursAgo: 5,
                CoverUrl: "https://images.unsplash.com/photo-1454165804606-c3d57bc86b40?auto=format&fit=crop&w=1280&q=80",
                Body: "<p>Khi cơ chế điều chỉnh biên giới carbon (CBAM) của Liên minh châu Âu bước vào giai đoạn áp dụng đầy đủ, các ngành thép, xi măng, nhôm, dệt may và da giày Việt Nam đang đẩy mạnh đầu tư điện mặt trời áp mái, kiểm kê khí nhà kính và chứng chỉ chuỗi cung ứng bền vững.</p><h2>Minh bạch dữ liệu phát thải trên từng lô hàng</h2><p>Nhiều doanh nghiệp sản xuất lớn tại Bình Dương, Đồng Nai và Hải Phòng đã triển khai hệ thống phần mềm đo đếm phát thải trực tiếp tại dây chuyền, kết nối dữ liệu năng lượng vào báo cáo ESG định kỳ. Điều này không chỉ giúp giảm nguy cơ bị áp thuế bổ sung tại cửa khẩu EU mà còn tạo lợi thế đàm phán đơn hàng dài hạn với các nhãn hàng toàn cầu.</p><blockquote>“Chuyển đổi xanh không còn là chi phí tuân thủ ngắn hạn mà đã trở thành giấy thông hành bắt buộc để doanh nghiệp Việt giữ vững thị phần tại các thị trường khó tính.” — Đại diện Hiệp hội Doanh nghiệp Xuất khẩu chia sẻ.</blockquote><h2>Tiếp cận dòng vốn tín dụng xanh</h2><p>Các ngân hàng thương mại hiện dành nhiều gói tín dụng ưu đãi lãi suất cho dự án thay thế lò hơi sinh khối, hệ thống xử lý nước thải tuần hoàn và nhà xưởng đạt chuẩn LEED. Doanh nghiệp nhỏ và vừa được khuyến nghị liên kết theo cụm ngành để chia sẻ chi phí kiểm toán carbon quốc tế.</p>"
            ),
            (
                Slug: "pin-the-ran-va-nang-luong-sach-buoc-ngoat-luu-tru-dien-nam-2026",
                Title: "Đột phá công nghệ pin thể rắn và hệ thống lưu trữ năng lượng quy mô lớn năm 2026",
                CategorySlug: "khoa-hoc",
                AuthorIdx: 0,
                CompanyIdx: (int?)2,
                ReadMin: 5,
                Views: 2180L,
                HoursAgo: 7,
                CoverUrl: "https://images.unsplash.com/photo-1532094349884-543bc11b234d?auto=format&fit=crop&w=1280&q=80",
                Body: "<p>Các phòng thí nghiệm vật liệu hàng đầu thế giới vừa công bố thế hệ pin thể rắn có mật độ năng lượng vượt mốc 450 Wh/kg, cho phép sạc 80% dung lượng chỉ trong vòng 10 phút và giảm thiểu nguy cơ cháy nổ nhờ chất điện phân rắn ổn định nhiệt.</p><h2>Giải bài toán lưu trữ cho điện gió và điện mặt trời</h2><p>Bên cạnh ứng dụng trên phương tiện giao thông, công nghệ pin natri-ion và pin dòng chảy đang được triển khai tại các trạm lưu trữ điện lưới (BESS) nhằm cân bằng công suất vào giờ cao điểm. Điều này giúp giảm đáng kể tình trạng cắt giảm công suất năng lượng tái tạo vào buổi trưa nắng gắt.</p><blockquote>“Khi chi phí lưu trữ năng lượng giảm xuống dưới 100 USD/kWh, điện mặt trời kết hợp pin lưu trữ sẽ cạnh tranh trực tiếp với nhiệt điện truyền thống về cả giá thành lẫn độ ổn định.” — PGS.TS Nguyễn Văn Hùng đánh giá.</blockquote>"
            ),
            (
                Slug: "xe-dien-do-thi-va-ha-tang-tram-sac-nhanh-phu-song-cao-toc-bac-nam",
                Title: "Xe điện đô thị bùng nổ: Hạ tầng trạm sạc nhanh phủ kín các trạm dừng nghỉ cao tốc Bắc – Nam",
                CategorySlug: "xe",
                AuthorIdx: 1,
                CompanyIdx: (int?)1,
                ReadMin: 5,
                Views: 3450L,
                HoursAgo: 4,
                CoverUrl: "https://images.unsplash.com/photo-1593941707882-a5bba14938c7?auto=format&fit=crop&w=1280&q=80",
                Body: "<p>Thị trường ô tô Việt Nam chứng kiến sự chuyển dịch mạnh mẽ sang xe thuần điện và hybrid khi chi phí vận hành tiết kiệm hơn 50% so với xe xăng truyền thống, đi kèm mạng lưới trạm sạc nhanh công suất 150–250 kW được lắp đặt đồng bộ dọc tuyến cao tốc Bắc – Nam.</p><h2>Xóa bỏ nỗi lo quãng đường khi đi liên tỉnh</h2><p>Nhờ tiêu chuẩn cổng sạc thống nhất và ứng dụng bản đồ hiển thị trạng thái trụ sạc trống theo thời gian thực, người dùng xe điện nay có thể thực hiện các hành trình xuyên Việt một cách chủ động. Các hãng xe cũng đẩy mạnh gói bảo hành pin 10 năm và dịch vụ cứu hộ lưu động 24/7.</p><ul><li>Tích hợp thanh toán tự động (Plug & Charge) rút ngắn thời gian thao tác tại trạm.</li><li>Mở rộng trạm sạc tại chung cư, trung tâm thương mại và bãi đỗ xe công cộng theo quy chuẩn phòng cháy mới.</li></ul>"
            ),
            (
                Slug: "tuyen-sinh-dai-hoc-2026-tang-cuong-danh-gia-nang-luc-va-nganh-ban-dan",
                Title: "Tuyển sinh đại học 2026: Mở rộng chỉ tiêu các ngành bán dẫn, AI và chuẩn hóa kỳ thi đánh giá năng lực",
                CategorySlug: "giao-duc",
                AuthorIdx: 0,
                CompanyIdx: (int?)0,
                ReadMin: 5,
                Views: 3890L,
                HoursAgo: 2,
                CoverUrl: "https://images.unsplash.com/photo-1541339907198-e08756dedf3f?auto=format&fit=crop&w=1280&q=80",
                Body: "<p>Mùa tuyển sinh đại học năm nay ghi nhận sức hút vượt trội ở nhóm ngành thiết kế vi mạch, trí tuệ nhân tạo, khoa học dữ liệu và năng lượng tái tạo, trong khi các trường đại học lớn tiến tới liên thông kết quả kỳ thi đánh giá năng lực để giảm áp lực thi cử cho thí sinh.</p><h2>Liên kết đào tạo thực chiến cùng doanh nghiệp</h2><p>Nhiều chương trình kỹ sư chất lượng cao đã đưa sinh viên năm 3 và năm 4 vào làm việc trực tiếp tại phòng lab doanh nghiệp từ 6 đến 12 tháng. Cách làm này giúp sinh viên tốt nghiệp rút ngắn thời gian đào tạo lại và nhanh chóng đáp ứng tiêu chuẩn dự án quốc tế.</p><blockquote>“Chọn ngành học cần xuất phát từ năng lực cốt lõi và khả năng tự học suốt đời, thay vì chỉ chạy theo xu hướng điểm chuẩn nhất thời.” — Ban tư vấn tuyển sinh khuyến nghị.</blockquote>"
            ),
            (
                Slug: "y-te-du-phong-va-ung-dung-ai-chan-doan-som-benh-ly-tim-mach",
                Title: "Ứng dụng AI trong chẩn đoán hình ảnh giúp phát hiện sớm nguy cơ đột quỵ và bệnh lý tim mạch",
                CategorySlug: "suc-khoe",
                AuthorIdx: 2,
                CompanyIdx: (int?)0,
                ReadMin: 5,
                Views: 2910L,
                HoursAgo: 6,
                CoverUrl: "https://images.unsplash.com/photo-1576091160399-112ba8d25d1d?auto=format&fit=crop&w=1280&q=80",
                Body: "<p>Nhiều bệnh viện tuyến trung ương và tuyến tỉnh tại Việt Nam đã đưa phần mềm trí tuệ nhân tạo hỗ trợ đọc phim CT, MRI và điện tâm đồ vào quy trình tầm soát, giúp bác sĩ rút ngắn thời gian phát hiện dấu hiệu tổn thương mạch máu não từ hàng chục phút xuống còn vài chục giây.</p><h2>Chủ động phòng bệnh từ thay đổi lối sống</h2><p>Các chuyên gia tim mạch nhấn mạnh công nghệ chẩn đoán chỉ phát huy tối đa hiệu quả khi người dân duy trì khám sức khỏe định kỳ 6–12 tháng một lần, kiểm soát huyết áp, mỡ máu, ngủ đủ giấc và duy trì vận động thể chất tối thiểu 150 phút mỗi tuần.</p>"
            ),
            (
                Slug: "du-lich-xanh-va-trai-nghiem-di-san-mien-trung-hut-khach-quoc-te",
                Title: "Du lịch xanh và tuyến tàu hỏa di sản miền Trung hút mạnh dòng khách quốc tế quay trở lại",
                CategorySlug: "du-lich",
                AuthorIdx: 2,
                CompanyIdx: null,
                ReadMin: 4,
                Views: 2470L,
                HoursAgo: 8,
                CoverUrl: "https://images.unsplash.com/photo-1528127269322-539801943592?auto=format&fit=crop&w=1280&q=80",
                Body: "<p>Hành trình kết nối Huế – Đà Nẵng – Hội An bằng tuyến tàu hỏa du lịch ngắm đèo Hải Vân cùng các tour trải nghiệm làng nghề không rác thải nhựa đang trở thành điểm sáng của du lịch Việt Nam, kéo dài thời gian lưu trú trung bình của du khách quốc tế.</p><h2>Nâng tầm dịch vụ từ bản sắc địa phương</h2><p>Thay vì chỉ khai thác du lịch nghỉ dưỡng đại trà, các địa phương đang đầu tư bài bản vào du lịch ẩm thực, bảo tồn kiến trúc cổ và không gian nghệ thuật buổi tối, mang lại nguồn thu bền vững cho cộng đồng dân cư bản địa.</p>"
            ),
            (
                Slug: "so-hoa-di-san-va-khong-gian-doc-sach-cong-dong-giua-long-do-thi",
                Title: "Số hóa di sản văn hóa và sức sống mới của các không gian đọc sách cộng đồng giữa lòng đô thị",
                CategorySlug: "van-hoa",
                AuthorIdx: 0,
                CompanyIdx: null,
                ReadMin: 4,
                Views: 1960L,
                HoursAgo: 10,
                CoverUrl: "https://images.unsplash.com/photo-1507842217343-583bb7270b66?auto=format&fit=crop&w=1280&q=80",
                Body: "<p>Những bảo tàng ứng dụng công nghệ quét 3D, thực tế tăng cường (AR) kết hợp chuỗi đường sách và thư viện mở cuối tuần đang kéo thế hệ trẻ đến gần hơn với lịch sử, văn học và nghệ thuật truyền thống dân tộc.</p><h2>Đưa di sản bước ra khỏi tủ kính</h2><p>Thông qua các triển lãm tương tác và dự án tái hiện cổ phục, âm nhạc dân gian trên nền tảng số, nhiều giá trị văn hóa lâu đời được kể lại bằng ngôn ngữ đương đại sinh động, tạo sức lan tỏa tích cực trong cộng đồng.</p>"
            ),
            (
                Slug: "dien-anh-viet-nam-vuon-ra-lien-hoan-phim-quoc-te-va-suc-bat-phong-ve",
                Title: "Điện ảnh Việt Nam khởi sắc: Sức bật phòng vé nội địa và dấu ấn mới tại các liên hoan phim quốc tế",
                CategorySlug: "giai-tri",
                AuthorIdx: 2,
                CompanyIdx: (int?)2,
                ReadMin: 4,
                Views: 2730L,
                HoursAgo: 5,
                CoverUrl: "https://images.unsplash.com/photo-1489599849927-2ee91cede3ba?auto=format&fit=crop&w=1280&q=80",
                Body: "<p>Làn sóng đạo diễn trẻ đầu tư nghiêm túc vào kịch bản gốc, kỹ xảo hậu kỳ và chất liệu văn hóa bản địa đã giúp nhiều tác phẩm điện ảnh Việt vừa chinh phục doanh thu phòng vé trong nước vừa liên tiếp góp mặt tại các liên hoan phim uy tín châu Á và châu Âu.</p><h2>Khán giả ngày càng khắt khe về chất lượng kịch bản</h2><p>Sự thành công của các bộ phim đa thể loại từ tâm lý xã hội, lịch sử đến hoạt hình 3D cho thấy khán giả sẵn sàng ủng hộ phim Việt khi tác phẩm được chăm chút chỉn chu và mang lại cảm xúc chân thật.</p>"
            ),
            (
                Slug: "bong-da-viet-nam-dau-tu-dao-tao-tre-va-chuan-hoa-cong-nghe-var",
                Title: "Bóng đá Việt Nam đẩy mạnh học viện đào tạo trẻ và phủ sóng công nghệ VAR toàn giải V-League",
                CategorySlug: "the-thao",
                AuthorIdx: 1,
                CompanyIdx: (int?)0,
                ReadMin: 5,
                Views: 4120L,
                HoursAgo: 3,
                CoverUrl: "https://images.unsplash.com/photo-1574629810360-7efbbe195018?auto=format&fit=crop&w=1280&q=80",
                Body: "<p>Việc áp dụng đồng bộ công nghệ VAR ở toàn bộ các vòng đấu kết hợp ứng dụng phân tích dữ liệu thể lực bằng cảm biến GPS tại các trung tâm đào tạo trẻ đang nâng cao rõ rệt chất lượng chuyên môn và tính công bằng của bóng đá chuyên nghiệp Việt Nam.</p><h2>Hướng tới mục tiêu bền vững ở đấu trường châu lục</h2><p>Các câu lạc bộ chú trọng hơn tới mặt sân đạt chuẩn, dinh dưỡng thể thao và tạo cơ hội thi đấu thường xuyên cho cầu thủ lứa U21 nhằm xây dựng lực lượng kế cận dày dặn cho đội tuyển quốc gia.</p>"
            ),
            (
                Slug: "ban-doc-hien-ke-giam-un-tac-gio-cao-diem-va-cai-tao-via-he-thong-thoang",
                Title: "Bạn đọc hiến kế giảm ùn tắc giờ cao điểm: Phân luồng linh hoạt và trả lại vỉa hè cho người đi bộ",
                CategorySlug: "ban-doc",
                AuthorIdx: 2,
                CompanyIdx: (int?)0,
                ReadMin: 4,
                Views: 2290L,
                HoursAgo: 7,
                CoverUrl: "https://images.unsplash.com/photo-1449824913935-59a10b8d2000?auto=format&fit=crop&w=1280&q=80",
                Body: "<p>Chuyên mục Đường dây nóng và Diễn đàn Bạn đọc tuần qua ghi nhận hàng trăm ý kiến đóng góp tâm huyết từ người dân Hà Nội và TP.HCM xoay quanh việc tối ưu chu kỳ đèn tín hiệu thông minh, lệch giờ học – giờ làm và quản lý trật tự vỉa hè minh bạch.</p><h2>Những đề xuất thiết thực từ thực tế đời sống</h2><ul><li>Tăng cường camera phạt nguội kết hợp biển báo điện tử cập nhật mật độ giao thông trước các nút giao lớn.</li><li>Quy hoạch bãi gửi xe kết nối trực tiếp nhà ga metro và trạm xe buýt để khuyến khích người dân bỏ xe cá nhân.</li><li>Công khai bản đồ các tuyến vỉa hè đủ điều kiện sử dụng tạm một phần và xử lý nghiêm tình trạng lấn chiếm lối đi bộ.</li></ul><p>Tòa soạn tiếp tục tổng hợp các góp ý xác đáng để chuyển tới cơ quan quản lý đô thị xem xét và phản hồi trực tiếp tới bạn đọc trong các bản tin tiếp theo.</p>"
            ),
        };

        var insertedCount = 0;
        foreach (var spec in articleSpecs)
        {
            var mediaPublicId = $"seed/covers/{spec.Slug}";
            var existingMedia = await dbContext.MediaFiles.FirstOrDefaultAsync(m => m.PublicId == mediaPublicId, cancellationToken);
            if (existingMedia is not null && existingMedia.Url != spec.CoverUrl)
            {
                existingMedia.Url = spec.CoverUrl;
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            var exists = await dbContext.Articles.AnyAsync(a => a.Slug == spec.Slug, cancellationToken);
            if (exists) continue;

            var author = journalists[spec.AuthorIdx];
            var media = existingMedia;
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
