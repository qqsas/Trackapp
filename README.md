# Show Tracker CLI

A simple command-line tool to track your TV shows: add shows, update episodes watched, rate them, and delete shows.  

## Features

- Add new shows with title, genre, and total episodes  
- View all shows in your database  
- Update episodes watched for a show  
- Rate a show  
- Delete a show  

## Requirements

- .NET 8+ (or use the self-contained executable below)  

## Installation
- Clone Repository
- Go to file location in terminal and do either of the bash code

1. Standard
```bash
dotnet tool install --global --add-source ./nupkg trackApp

#For macOS and linux onlu
export PATH="$PATH:$HOME/.dotnet/tools"
```

2. Build and publish the app:
```bash
#linux
dotnet publish -c Release -r linux-x64 --self-contained true -o ~/bin/track_app

#windows
dotnet publish -c Release -r win-x64 --self-contained false -o ./publish

#any OS
dotnet tool install --global --add-source ./nupkg trackApp

Make it executable:
chmod +x ~/bin/track_app/track
mv ~/bin/track_app/track ~/bin/track

track
