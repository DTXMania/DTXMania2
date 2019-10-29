
このプロジェクトは、.hlsl ファイルをビルドして .cso ファイルを出力します。
HLSLのビルドは C++ プロジェクトでしかできないので、個別にプロジェクトを設けました。

また、以下のビルド後イベントを実行して、DTXMania2 プロジェクトに .cso ファイルをコピーします。

xcopy $(OutDir)*.cso $(SolutionDir)DTXMania2\Resources\Default\Images /Y /D

