# NewsHub - AI-Powered News Platform

![Version](https://img.shields.io/badge/version-1.0.0-blue.svg)
![License](https://img.shields.io/badge/license-MIT-green.svg)
![Build](https://img.shields.io/badge/build-passing-brightgreen.svg)

NewsHub is an intelligent news aggregator and reading platform designed to combat information overload. By leveraging Large Language Models (LLMs), NewsHub automatically categorizes, summarizes, and personalizes news articles, delivering a clean, focused, and time-saving reading experience.

---

## 1. Problem & Solution

**The Problem:**

- Information Overload: Users are overwhelmed by thousands of articles published daily.
- Clickbait & Fluff: Readers waste time on lengthy articles that contain very little actual value.
- Manual Categorization: Traditional news platforms spend significant editorial resources on tagging and categorizing content.

**The Solution:**

- Fully automated content pipeline: Analyze -> Categorize -> Summarize.
- AI-generated "TL;DR" summaries allowing users to grasp the core message of an article in seconds.
- Semantic search capabilities that understand user intent rather than relying strictly on exact keyword matching.

---

## 2. Core Features

### Business Logic

- Authentication & Authorization: Secure login/registration using JWT. Role-based access control (Admin, Journalist, User).
- Article Management (CRUD): Authorized users can Create, Read, Update, and Delete articles. Includes Rich Text Editor support.
- User Dashboard: Manage profile settings, bookmark favorite articles, and view reading history.
- System Dashboard: Admin analytics regarding total articles, user engagement, and automated tagging success rates.

### AI-Powered Features

- Auto-Categorization: When an article is saved, the AI analyzes the content and automatically assigns relevant categories.
- Smart Summarization: An AI-driven feature that extracts the most critical points of an article into a concise bulleted list.
- Intelligent Search: Natural language processing search that returns contextually relevant results.

---

## 3. Technology Stack

This project is built using a modern, enterprise-grade technology stack:

### Development

- Frontend: TypeScript, React, TailwindCSS.
- Backend: C# ASP.NET 10.
- Database: PostgreSQL.
- AI Integration: Google Gemini API.
- Architecture: Clean Architecture (Backend).

### DevOps & Tools

- IDE: Visual Studio Code.
- Project Management: Linear.
- Source Control: GitHub.
- AI Code Review: CodeRabbit AI integrated into GitHub PRs.
- CI/CD & Containerization: GitHub Actions, Docker.
- Deployment: Vercel (Frontend), Render (Backend & Database).

---

## 4. System Architecture & Folder Structure

The repository is structured as a Monorepo containing both the Frontend and Backend. The backend strictly follows **Clean Architecture** principles.

```text
newshub/
├── .github/workflows/          # CI/CD pipelines (GitHub Actions)
├── frontend/                   # React TypeScript Application
│   ├── public/                 # Static assets
│   └── src/                    # Frontend source code
│       ├── assets/             # Images, global styles
│       ├── components/         # Reusable UI components
│       ├── hooks/              # Custom React hooks
│       ├── pages/              # Page components (Routing)
│       ├── services/           # API integration (Axios/Fetch)
│       ├── store/              # State management
│       ├── types/              # TypeScript interfaces/types
│       └── utils/              # Helper functions
├── backend/                    # ASP.NET 10 Solution
│   ├── NewsHub.sln             # Visual Studio Solution File
│   ├── src/                    # Backend source code
│   │   ├── NewsHub.Domain/         # Entities, Value Objects, Domain Interfaces (No Dependencies)
│   │   ├── NewsHub.Application/    # Use Cases, CQRS, DTOs, Validation (Depends on Domain)
│   │   ├── NewsHub.Infrastructure/ # EF Core, DB Context, Gemini API Service (Depends on App)
│   │   └── NewsHub.API/            # Controllers, Middlewares, Program.cs (Depends on App & Infra)
│   ├── tests/                  # Unit and Integration Tests
│   └── Dockerfile              # Backend containerization configuration
├── docker-compose.yml          # Local development environment setup
└── README.md                   # Project documentation
```
