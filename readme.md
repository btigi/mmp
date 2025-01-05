## Introduction

My Media Player (mmp) is command line audio player, powered by naudio.

## Download

Compiled downloads are not available.

## Compiling

To clone and run this application, you'll need [Git](https://git-scm.com) and [.NET](https://dotnet.microsoft.com/) installed on your computer. From your command line:

```
# Clone this repository
$ git clone https://github.com/btigi/mmp

# Go into the repository
$ cd src

# Build  the app
$ dotnet build
```

## Usage

```mmp```

`a` - add new track (maintain playlist), supports tab completion

`n` - add new track (clear playlist), supports tab completion

`space` - pause / unpause

`s` - stop playback

`p` - begin playback

![screenshot showing output](resources/screenshot.png)


## Licencing

mmp is licenced under CC BY-NC-ND 4.0 https://creativecommons.org/licenses/by-nc-nd/4.0/ Full licence details are available in licence.md