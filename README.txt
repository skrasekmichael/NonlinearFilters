Projekt obsahuje 2 demo aplikace:
=================================

Okenní demonstrační aplikace (GUI):
-----------------------------------
Přeložená se nachází v adresáři: ./Demo/GUI/NonlinearFilters.APP.exe
Pro práci je nutné otevřít data, vzorová data se nachází v adresáři: ./Data
Aplikaci lze také  spusit příkazem: make demo app
Aplikaci lze také rovnou přeložit a spustit příkazem: make run

Konzolová aplikace (CLI):
-------------------------
Přeložená pro Windows se nachází v adresáři: ./Demo/CLI/NonlinearFilters.CLI.exe, 
pro spuštění na jiných platfromách je nunté projekt přeložit (make cli), přeložená
aplikace se pak nachází v klasickém adresáři dotnet: ./Sources/NonlinearFilters.CLI/bin/...

příkazem s parametrem --help lze zobrazit rozhraní aplikace (CLI)
.\NonlinearFilters.CLI.exe --help

pro demonstraci CLI aplikace také lze využít testovací skript: ./test.ps1,
který spustí CLI aplikaci s vzorovými daty a přednastavenými parametry,
skript lze také spustit příkazem: make test
Pokud sysrtém nemá nainstalovaný PowerShell v7, je nutné testovací skript spustit ručně. 

Vyfiltrovaná data jsou po aplikaci demonstračního skriptu ve složce: ./Data/demo
data je možné zobrazit pomocí programů 3. stran (3D sliceru), implmentované filty ukládají do složky 
aji náhled volumetrických dat.
