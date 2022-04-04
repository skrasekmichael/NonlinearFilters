SRC=Sources
CLI=$(SRC)/NonlinearFilters.CLI
APP=$(SRC)/NonlinearFilters.APP
FLAGS=-c Release

all:
	dotnet build $(SRC) $(FLAGS)

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
	pwsh -NoProfile test.ps1 $(name)
