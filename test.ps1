$cli = "Sources/NonlinearFilters.CLI/bin/Release/net6.0/NonlinearFilters.CLI"
$python = "python"
$testName = $args[0]

function wrn($message) {
	Write-Host "WARNING $message" -ForegroundColor Yellow
}

function wrn_time {
	wrn "This might take some time!";
}

function wrn_cmp_not_supported {
	wrn "PowerShell Script 'cmp-img' for comparing images is not supported/located on this system!"
}

function wrn_join_not_supported {
	wrn "PowerShell Script 'join-img' for merging images into grid image is not supported/located on this system!"
}

function err($message) {
	Write-Host "ERROR $message" -ForegroundColor Red
}

function run($params) {
	$cpu = (Start-Process -FilePath $cli -ArgumentList $params -Wait -PassThru -NoNewWindow).CPU
	if ($IsWindows) {
		Write-Host "CPU time [s]: $cpu"
	}
}

function python_script {
	param (
		[string]$File,
		[string]$Params
	)
	
	$cpu = (Start-Process -FilePath $python -ArgumentList "Python/$File $Params" -Wait -PassThru -NoNewWindow).CPU
	if ($IsWindows) {
		Write-Host "CPU time [s]: $cpu"
	}
}

function run_list($list) {
	foreach ($param in $list) {
		run($param)
		Write-Host ""
	}
}

function bilateral {
	#bilateral (space sigma, range sigma, r)
	run_list(
		"-i Data/noisy.png -o Data/bilateral/bilateral.png -f bf -p `"6, 25.5, -1`"",
		"-i Data/noisy.png -o Data/bilateral/bilateral-fast-1-thread.png -f fbf -tc 1 -p `"6, 25.5, -1`"",
		"-i Data/noisy.png -o Data/bilateral/bilateral-fast.png -f fbf -p `"6, 25.5, -1`""
	)

	python_script -File opencv-bl.py -Params "Data/noisy.png Data/bilateral/opencv.png 15 6 25.5"
	Write-Host ""

	python_script -File sitk-bl.py -Params "Data/noisy.png Data/bilateral/sitk.png 6 25.5"
	Write-Host ""

	python_script -File itk-bl.py -Params "Data/noisy.png Data/bilateral/itk.png 2 6 25.5"
	Write-Host ""

	join_img -Width 400 -Cols 2 -Output "Images/bl-noisy-vs-bilateral.png" -Files "Data/noisy.png", "Data/bilateral/bilateral-fast.png"
	Write-Host ""

	cmp_img -I1 "Data/bilateral/bilateral-fast.png" -I2 "Data/bilateral/bilateral.png" -Out "Data/bilateral/bl-diff.png" -Zoom 30 -GS true
}


function bl_grid {
	$rangeSigma = 10, 25, 50
	$spaceSigma = 10, 25, 40

	$files = [System.Collections.ArrayList]::new()
	$params = [System.Collections.ArrayList]::new()

	for ($i = 0; $i -lt $rangeSigma.Count; $i++) {
		for ($j = 0; $j -lt $spaceSigma.Count; $j++) {
			$rSigma = $rangeSigma[$i]
			$sSigma = $spaceSigma[$j]
			$fileName = "Data/bl-grid/range-$rSigma-spatial-$sSigma.png";
			$files.Add($fileName) | Out-Null

			#bilateral (space sigma, range sigma, r)
			$params.Add("$sSigma, $rSigma, -1") | Out-Null
		}
	}

	$filesString = $files | Join-String -Separator ", "
	$paramString = $params | Join-String -Separator ", "

	run("-i Data/target.png -o `"$filesString`" -f fbf -t Param -p `"$paramString`"");

	$cols = $spaceSigma | ForEach-Object { "σₛ=$_" }
	$rows = $rangeSigma | ForEach-Object { "σᵣ=$_" }
	join_img -Width 300 -Cols 3 -Space 5 -Output "Data/bl-grid/grid.png" -Files $files -FontName "Cambria" -ColTitles $cols -Top 20 -RowTitles $rows -Left 40
}

function nlmeans {
	#nl-means (patch radius, window radius, h)
	run_list(
		"-i Data/noisy.png -o Data/nlmeans/nlmeans-pixel.png -f nlmf -p `"1, 10, 5`"",
		"-i Data/noisy.png -o Data/nlmeans/nlmeans-patch.png -f nlmpf -p `"1, 10, 15, -1`"",
		"-i Data/noisy.png -o Data/nlmeans/nlmeans-patch-sampled.png -f nlmpf -p `"1, 10, 15, 500`"",
		"-i Data/noisy.png -o Data/nlmeans/nlmeans-fast-1-thread.png -f fnlmf -tc 1 -p `"1, 10, 15, -1`"",
		"-i Data/noisy.png -o Data/nlmeans/nlmeans-fast.png -f fnlmf -p `"1, 10, 15, -1`"",
		"-i Data/noisy.png -o Data/nlmeans/nlmeans-fast-sampled.png -f fnlmf -p `"1, 10, 15, 500`""
	)

	python_script -File opencv-nlm.py -Params "Data/noisy.png Data/nlmeans/opencv.png 1 10 15"
	Write-Host ""

	join_img -Width 400 -Cols 2 -Output "Images/nlm-noisy-vs-pixel.png" -Files "Data/noisy.png", "Data/nlmeans/nlmeans-pixel.png"
	Write-Host ""

	join_img -Width 400 -Cols 2 -Output "Images/nlm-noisy-vs-patch.png" -Files "Data/noisy.png", "Data/nlmeans/nlmeans-patch.png"
	Write-Host ""

	join_img -Width 400 -Cols 2 -Output "Images/nlm-fast-vs-opencv.png" -Files "Data/nlmeans/nlmeans-fast.png", "Data/nlmeans/opencv.png"
	Write-Host ""
	
	cmp_img -I1 "Data/nlmeans/nlmeans-patch.png" -I2 "Data/nlmeans/nlmeans-fast.png" -Out "Data/nlmeans/nlm-patch-vs-fast-diff.png" -Zoom 30 -GS true
	cmp_img -I1 "Data/nlmeans/opencv.png" -I2 "Data/nlmeans/nlmeans-fast.png" -Out "Data/nlmeans/nlm-fast-vs-opencv-diff.png" -Zoom 30 -GS true
}

function cmp_2d_filters {
	run_list(
		"-i Data/noisy2.png -o Data/2d-cmp/bilateral1.png -f fbf -p `"30, 50, -1`"",
		"-i Data/noisy2.png -o Data/2d-cmp/bilateral2.png -f fbf -p `"30, 100, -1`"",
		"-i Data/noisy2.png -o Data/2d-cmp/nlmeans-patch.png -f fnlmf -p `"1, 10, 40, -1`""
	)

	join_img -Width 300 -Cols 4 -Output "Images/2d-cmp.png" -Space 5 `
		-Files "Data/noisy2.png", "Data/2d-cmp/bilateral1.png", "Data/2d-cmp/bilateral2.png", "Data/2d-cmp/nlmeans-patch.png" `
		-ColTitles "noisy", "bilateral 30 space, 50 range", "bilateral 30 space, 100 range", "non-local means 3x3, 21x21, 40 h" -Top 20
}

function bl3d {
	run_list(
		"-i Data/foot-noisy.nrrd -o Data/bl3d/foot-bl-1-thread.nrrd -f fbf3 -tc 1 -p `"5, 20, -1`"",
		"-i Data/foot-noisy.nrrd -o Data/bl3d/foot-bl.nrrd -f fbf3 -p `"5, 20, -1`""
	)

	wrn_time
	python_script -File sitk-bl.py -Params "Data/foot-noisy.nrrd Data/bl3d/foot-sitk.nrrd 5 20"

	wrn_time
	python_script -File itk-bl.py -Params "Data/foot-noisy.nrrd Data/bl3d/foot-itk.nrrd 3 5 20"
}

function nlm3d {
	run_list(
		"-i Data/c60-noisy.nrrd -o Data/nlm3d/c60-nlm-1-thread.nrrd -f fnlmf3 -tc 1 -p `"1, 7, 20, -1`"",
		"-i Data/c60-noisy.nrrd -o Data/nlm3d/c60-nlm.nrrd -f fnlmf3 -p `"1, 7, 20, -1`""
	)

	wrn_time
	run("-i Data/foot-noisy.nrrd -o Data/nlm3d/foot-nlm-sampled.nrrd -f fnlmf3 -p `"1, 7, 20, 500`"")

	wrn_time
	run("-i Data/foot-noisy.nrrd -o Data/nlm3d/foot-nlm.nrrd -f fnlmf3 -p `"1, 7, 20, -1`"")

	wrn_time
	python_script -File scikit-nlm.py -Params "Data/foot-noisy.nrrd Data/nlm3d/foot-scikit-nlm.nrrd 1 7 20"
}

$testTemplates = [ordered]@{ 
	"all" =
		$Function:bilateral,
		$Function:nlmeans,
		$Function:cmp_2d_filters,
		$Function:bl3d,
		$Function:nlm3d;
	"default" =
		$Function:bilateral,
		$Function:nlmeans,
		$Function:cmp_2d_filters;
	"bl" = $Function:bilateral;
	"bl grid" = $Function:bl_grid;
	"nlm" = $Function:nlmeans;
	"2d cmp" = $Function:cmp_2d_filters;
	"bl 3d" = $Function:bl3d;
	"nlm 3d" = $Function:nlm3d;
}

function get_tests {
	param(
		[string]$Name
	)

	$Name = $Name.ToLower() -replace "\s",""
	if ($Name -eq "") {
		$Name = "default"
	}

	$keys = $testTemplates.Keys | ForEach-Object { $_ -replace "\s","" }
	$index = [array]::IndexOf($keys, $Name);

	if ($index -gt -1) {
		return ([array]$testTemplates.Values)[$index]
	} else {
		err "Unrecognized name of test template [$Name]"
		Write-Host "Expected one of the following names: "
		$testTemplates.Keys | ForEach-Object { " - $_" }
		exit
	}
}

if ((Test-Path $cli -PathType Leaf) -eq $False) {
	err "CLI path not found [$cli]";
	exit
}

$runCmp = [bool](Get-Command cmp-img -ErrorAction SilentlyContinue)
$runJoin = [bool](Get-Command join-img -ErrorAction SilentlyContinue)

if ($IsLinux) {
	$python = "python3"
	$runJoin = $False;
	$runCmp = $False;
}

if ($IsWindows) {
	$cli = "$cli.exe"
}

if ($runCmp) {
	Set-Alias -Name cmp_img -Value cmp-img
} else {
	Set-Alias -Name cmp_img -Value wrn_cmp_not_supported
}

if ($runJoin) {
	Set-Alias -Name join_img -Value join-img
} else {
	Set-Alias -Name join_img -Value wrn_join_not_supported
}

$tests = get_tests -Name $testName

$line = New-Object -TypeName string -ArgumentList '-', 80
foreach ($test in $tests) {
	Write-Host $line
	$test.Invoke()
}
Write-Host $line
