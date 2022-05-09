SRC=Sources
CLI=$(SRC)/NonlinearFilters.CLI
APP=$(SRC)/NonlinearFilters.APP
FLAGS=-c Release

all:
ifeq ($(OS),Windows_NT)
	dotnet build $(SRC) $(FLAGS)
endif

cli:
	dotnet build $(CLI) $(FLAGS)

run:
	dotnet run --project $(APP) $(FLAGS)

clean:
	dotnet restore $(SRC)
	dotnet clean $(SRC)
	dotnet restore $(CLI)
	dotnet clean $(CLI)

test:
	pwsh -NoProfile test.ps1 -TestName "$(name)"

demoapp:
	./Demo/GUI/NonlinearFilters.APP.exe

copydemos:
ifeq ($(OS),Windows_NT)
	xcopy /E /H /C /Y /I .\Sources\NonlinearFilters.CLI\bin\Release\net6.0 .\Demo\CLI
	xcopy /E /H /C /Y /I .\Sources\NonlinearFilters.APP\bin\Release\net6.0-windows .\Demo\GUI
else
	cp -R Sources/NonlinearFilters.CLI/bin/Release/net6.0/ Demo/CLI/
endif

demos: all cli copydemos
