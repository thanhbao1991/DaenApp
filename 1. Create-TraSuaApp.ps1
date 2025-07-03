# Tạo solution
dotnet new sln -n TraSuaApp

# Tạo project
dotnet new classlib -n TraSuaApp.Domain
dotnet new classlib -n TraSuaApp.Application
dotnet new classlib -n TraSuaApp.Infrastructure
dotnet new classlib -n TraSuaApp.Shared
dotnet new wpf -n TraSuaApp.WpfClient
dotnet new webapi -n TraSuaApp.Api

# Thêm vào solution
dotnet sln TraSuaApp.sln add TraSuaApp.Domain/
dotnet sln TraSuaApp.sln add TraSuaApp.Application/
dotnet sln TraSuaApp.sln add TraSuaApp.Infrastructure/
dotnet sln TraSuaApp.sln add TraSuaApp.Shared/
dotnet sln TraSuaApp.sln add TraSuaApp.WpfClient/
dotnet sln TraSuaApp.sln add TraSuaApp.Api/

# Kết nối các reference
dotnet add TraSuaApp.Application reference TraSuaApp.Domain

dotnet add TraSuaApp.Infrastructure reference TraSuaApp.Domain
dotnet add TraSuaApp.Infrastructure reference TraSuaApp.Shared

dotnet add TraSuaApp.WpfClient reference TraSuaApp.Application
dotnet add TraSuaApp.WpfClient reference TraSuaApp.Domain
dotnet add TraSuaApp.WpfClient reference TraSuaApp.Infrastructure
dotnet add TraSuaApp.WpfClient reference TraSuaApp.Shared

dotnet add TraSuaApp.Api reference TraSuaApp.Application
dotnet add TraSuaApp.Api reference TraSuaApp.Domain
dotnet add TraSuaApp.Api reference TraSuaApp.Infrastructure
dotnet add TraSuaApp.Api reference TraSuaApp.Shared

# Thêm EF Core packages vào Infrastructure
dotnet add TraSuaApp.Infrastructure package Microsoft.EntityFrameworkCore
dotnet add TraSuaApp.Infrastructure package Microsoft.EntityFrameworkCore.SqlServer
dotnet add TraSuaApp.Infrastructure package Microsoft.EntityFrameworkCore.Design