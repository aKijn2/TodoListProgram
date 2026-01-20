# 📋 TaskFlow

A modern task management application built with **.NET MAUI 9.0** for Windows. Manage your tasks locally with a clean, mobile-inspired interface.

## ✨ Features

- **Task Management** - Create, edit, and delete tasks
- **Subtasks** - Break down tasks into smaller steps
- **Status Tracking** - To Do → In Progress → Completed
- **Due Dates** - Set optional deadlines with overdue alerts
- **Filtering** - View tasks by status
- **Local Storage** - SQLite database, no cloud required
- **Dark Theme** - Modern, eye-friendly interface

## 🛠️ Tech Stack

- .NET MAUI 9.0
- SQLite (sqlite-net-pcl)
- MVVM with CommunityToolkit.Mvvm
- C# 12

## 🚀 Getting Started

### Prerequisites

- .NET 9.0 SDK
- Visual Studio 2022 or VS Code
- Windows 10/11

### Run the App

```bash
cd Todo_asa
dotnet restore
dotnet run -f net9.0-windows10.0.19041.0
```

## 📁 Project Structure

```
Todo_asa/
├── Models/          # Data models (TaskItem, SubTaskItem)
├── ViewModels/      # MVVM ViewModels
├── Pages/           # UI pages
├── Services/        # Database service
├── Converters/      # XAML value converters
└── Resources/       # Styles, colors, fonts
```

## 📄 License

MIT License - Feel free to use and modify!

---

Made with ❤️ using .NET MAUI
