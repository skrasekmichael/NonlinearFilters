$cli = "Sources/NonlinearFilters.CLI/bin/Release/net6.0/NonlinearFilters.CLI.exe"

function run($params) {
	Start-Process -FilePath $cli -ArgumentList $params -Wait -NoNewWindow
}

function python_script {
	param (
		[string]$File,
		[string]$Params
	)
	
	Start-Process -FilePath "python" -ArgumentList "Python/$File $Params" -Wait -NoNewWindow
}

function run_list($list) {
	foreach ($param in $list) {
		run($param)
		Write-Host ""
	}
}

function bilateral {
	#bilateral (space sigma, range sigma)
	$params = 
		"-i Images/noisy.png -o Images/bilateral/bilateral.png -f bf -p `"6, 25.5, -1`"",
		"-i Images/noisy.png -o Images/bilateral/bilateral-fast.png -f fbf -p `"6, 25.5, -1`""

	run_list($params)

	python_script -File opencv-bl.py -Params "Images/noisy.png Images/bilateral/opencv.png 15 25.5 6"
	Write-Host ""

	python_script -File sitk-bl.py -Params "Images/noisy.png Images/bilateral/sitk.png 25.5 6"
	Write-Host ""

	join-img -Width 400 -Cols 2 -Output "Images/bl-noisy-vs-bilateral.png" -Files "Images/noisy.png", "Images/bilateral/bilateral-fast.png"
	Write-Host ""

	cmp-img -I1 "Images/bilateral/bilateral-fast.png" -I2 "Images/bilateral/bilateral.png" -Out "Images/bl-diff.png" -Zoom 30 -GS true
}

function nlmeans {
	#nl-means (patch radius, window radius, h)
	$params =
		"-i Images/noisy.png -o Images/nlmeans/nlmeans-pixel.png -f nlmf -p `"1, 10, 5, 0`"",
		"-i Images/noisy.png -o Images/nlmeans/nlmeans-patch.png -f nlmf -p `"1, 10, 15, 1`"",
		"-i Images/noisy.png -o Images/nlmeans/nlmeans-fast.png -f fnlmf -p `"1, 10, 15`""

	run_list($params)

	python_script -File opencv-bl.py -Params "Images/noisy.png Images/nlmeans/opencv.png 1 10 20"
	Write-Host ""

	join-img -Width 400 -Cols 2 -Output "Images/nlm-noisy-vs-pixel.png" -Files "Images/noisy.png", "Images/nlmeans/nlmeans-pixel.png"
	Write-Host ""

	join-img -Width 400 -Cols 2 -Output "Images/nlm-noisy-vs-patch.png" -Files "Images/noisy.png", "Images/nlmeans/nlmeans-patch.png"
}

function fnlm_grid_patch_h {
	$radius = 1, 2, 3
	$h = 15, 35, 55

	$out = [System.Collections.ArrayList]::new()
	$params = [System.Collections.ArrayList]::new()

	for ($y = 0; $y -lt $radius.Count; $y++) {
		for ($x = 0; $x -lt $h.Count; $x++) {
			$d = $radius[$y] * 2 + 1
			$out.Add("Images/fnlmf-param-grid/$($d)x$($d)-$($h[$x]).png") | Out-Null
			#nl-means (patch radius, window radius, h)
			$params.Add("$($radius[$y]), 10, $($h[$x])") | Out-Null
		}
	}

	run("-i Images/noisy.png -o `"$($out -join ',')`" -f fnlmf -t 1 -p `"$($params -join ',')`"")

	join-img -Width 400 -Cols $h.Count -Output "Images/fnml-param-grid.png" -Files $out
}

function edge_preservation {
	$params =
		"-i Images/noisy2.png -o Images/edge/bilateral1.png -f fbf -p `"30, 50, -1`"",
		"-i Images/noisy2.png -o Images/edge/bilateral2.png -f fbf -p `"30, 100, -1`"",
		"-i Images/noisy2.png -o Images/edge/nlmeans-patch.png -f fnlmf -p `"1, 10, 40`""

	run_list($params)

	join-img -Width 300 -Cols 4 -Output "Images/edge-preservation.png" `
		-Files "Images/noisy2.png", "Images/edge/bilateral1.png", "Images/edge/bilateral2.png", "Images/edge/nlmeans-patch.png" `
		-ColTitles "noisy", "bilateral 30 space, 50 range", "bilateral 30 space, 100 range", "non-local means 3x3, 21x21, 40 h"
}

if ((Test-Path $cli -PathType Leaf) -eq $False) {
	Write-Error "CLI path not found [$cli]"
	exit
}

$tests =
	$Function:bilateral,
	$Function:nlmeans,
	$Function:fnlm_grid_patch_h,
	$Function:edge_preservation

$line = New-Object -TypeName string -ArgumentList '-', 80
foreach ($test in $tests) {
	Write-Host $line
	$test.Invoke()
}
Write-Host $line
