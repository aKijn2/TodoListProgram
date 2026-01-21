# TaskFlow

A minimalist, high-performance task management application built with **.NET MAUI 9.0** for Windows. Designed with a strict monochrome aesthetic for distraction-free productivity.

## Features

### Minimalist Design
- **Monochrome Theme**: Black & white interface with glassmorphism elements.
- **Distraction-Free**: Clean typography and layouts focused on content.
- **Micro-interactions**: Subtle hover effects and responsive UI elements.

### Core Functionality
- **Search & Filter**: Instantly search tasks by title/description with a sleek search bar.
- **Task Management**: Create, edit, and delete tasks with ease.
- **Subtasks**: Break complex tasks into manageable sub-steps.
- **Smart Filtering**: "To Do", "Active", and "Done" tabs for quick navigation.
- **Due Dates**: Set deadlines and track overdue items.

### Technical Highlights
- **Local Storage**: Secure, offline-first SQLite database (`Documents/Todo_asa` storage).
- **AOT Compatible**: Fully optimized ViewModels avoiding reflection-heavy MVVM features.
- **Performance**: Instant load times and smooth transitions.

## Tech Stack

- **Framework**: .NET MAUI 9.0
- **Database**: SQLite (sqlite-net-pcl)
- **Architecture**: MVVM (CommunityToolkit.Mvvm)
- **Language**: C# 12 / XAML

## Getting Started

### Prerequisites

- .NET 9.0 SDK
- Visual Studio 2022 or VS Code

### Run the App

```bash
cd Todo_asa
dotnet restore
dotnet run -f net9.0-windows10.0.19041.0
```

## 📁 Project Structure

```
TodoListProgram/
├── Models/          # Data models (TaskItem, SubTaskItem)
├── ViewModels/      # MVVM ViewModels (AOT optimized)
├── Pages/           # UI pages (MainPage, TaskDetailPage)
├── Services/        # Database service logic
└── Resources/       # Styles, colors, fonts
```

## 📄 License

MIT License - Feel free to use and modify!

---

Made with ❤️ and .NET MAUI
