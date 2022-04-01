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

test:
	pwsh -NoProfile test.ps1 $(name)
