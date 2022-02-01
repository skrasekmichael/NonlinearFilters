SRC=Sources
CLI=$(SRC)/NonlinearFilters.CLI
FLAGS=-c Release

all:
	dotnet build $(SRC) $(FLAGS)

cli:
	dotnet build $(CLI) $(FLAGS)

test:
	pwsh test.ps1
