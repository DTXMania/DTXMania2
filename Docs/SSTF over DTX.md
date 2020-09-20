# SSTF over DTX 

SSTFormat は DTXMania2 独自の規格であり無印やVer.Kでは演奏できないため、「DTXMania用の譜面」をうたって配布することは誤解を招きかねない。

そこで、SSTFormat を「従来の DTXMania でも演奏可能なDTX形式」で表現することを考える。

## 識別子

SSTF over DTX は、拡張子として `.dtx` を使うが、通常の DTX と識別するために、１行目に特別な識別子「`SSTF over DTX`」を必要とする。

```
# SSTF over DTX, SSTFVersion 4.1.0.0
```

## ドラムサウンド

SSTF を DTX に変換するにあたり、ドラムサウンドファイルを準備する必要がある。
SSTF over DTX では、以下の構造でドラムサウンドファイルが**配置されているもの**とみなす。（これは、DTXMania2 の "`Recources/Default/DrumsSounds/`" と同じである。）

```
DrumSounds/
    Bass.wav
    China.wav
    HiHatClose.wav
    HiHatFoot.wav
    HiHatHalfOpen.wav
    HiHatOpen.wav
    LeftCrash.wav
    LeftCymbalMute.wav
    Ride.wav
    RideCup.wav
    RightCrash.wav
    RightCymbalMute.wav
    Snare.wav
    SnareClosedRim.wav
    SnareGhost.wav
    SnareOpenRim.wav
    Splash.wav
    Tom1.wav
    Tom1Rim.wav
    Tom2.wav
    Tom2Rim.wav
    Tom3.wav
    Tom3Rim.wav
```

## SSTF から SSTFoverDTX への変換

### ヘッダ行

| SSTFormat.スコア          |    | SSTFoverDTX           | 備考      |
|---------------------------|----|-----------------------|-----------|
| 曲名                      | → | #TITLE                |           |
| アーティスト名            | → | #ARTIST               |           |
| 説明文                    | → | #COMMENT              |           |
| 難易度                    | → | #DLEVEL               |           |
| プレビュー音声ファイル名  | → | #PREVIEW              |           |
| プレビュー画像ファイル名  | → | #PREIMAGE             |           |
| BGVファイル名             | → | #VIDEO01              | 01 で固定 |
| BGMファイル名             | → | #WAVC0, #BGMWAV: C0   | C0 で固定 |


### ドラムチップ

チップ種別と音量の組み合わせにより、以下の WAV 番号に振り分ける。
各WAVに対しては、音量に応じた #VOLUME が設定される。
（※音量0～7 は、SSTFEditor の -6～+1 に相当。）

|チップ種別         |DTX ch.|音量0～7に対応する WAV の zz|備考|
|-------------------|----|--------|-----|
|LeftCrash          | 1A | 10～17 |     |
|LeftCrash Mute     | 61 | 18～1F |DTXではSE扱い   |
|HiHat              | 11 | 20～27 |     |
|HiHat (Open)       | 18 | 28～2F |     |
|HiHat (HalfOpen)   | 18 | 2G～2N |     |
|FootPedal          | 1B | 2O～2V |     |
|Snare              | 12 | 30～37 |     |
|Snare (OpenRim)    | 12 | 38～3F |     |
|Snare (ClosedRim)  | 12 | 3G～3N |     |
|Snare (Ghost))     | 12 | 3O～3V |     |
|Bass               | 13 | 40～47 |     |
|LeftBass           | 1C | 48～4F |     |
|Tom1               | 14 | 50～57 |     |
|Tom1 (Rim)         | 14 | 58～5F |     |
|Tom2               | 15 | 60～67 |     |
|Tom2 (Rim)         | 15 | 68～6F |     |
|Tom3               | 17 | 70～77 |     |
|Tom3 (Rim)         | 17 | 78～7F |     |
|RightCrash         | 16 | 80～87 |     |
|RightCrash Mute    | 62 | 88～8F |DTXではSE扱い    |
|Ride               | 19 | 90～97 |     |
|Ride (Cup)         | 19 | 98～9F |     |
|China              | 16 | A0～A7 |右レーン固定     |
|Splash             | 1A | B0～B7 |左レーン固定     |
|背景動画            | 5A | 01     |全画面動画       |
|BGM                | 01 | C0     |                 |
|小節長倍率          | 02 | N/A    |                 |

小節長倍率は、SSTF では指定された小節にのみ適用されるが、DTX ではそれ以降すべての小節に適用されるため、追加の小節長倍率チップの配置が必要。

BPM チップはすべて DTX の #BPMzz に展開し、ch.08 を使って配置する。


## SSTFoverDTX からの SSTF の復元

SSTFoverDTX ファイルは、まずは通常の DTX ファイルとして読み込まれる。
その上で、一行目に「SSTF over DTX」識別子がある場合は、次の変換が適用される。

| チップ種別    | サブチップID(zz) |  | 新チップ種別 |
|---------------|--------|----|----------------------|
| SE1           | 18～1F | → | LeftCymbal_Mute      |
| HiHat_Close   | 28～2F | → | HiHat_Open           |
| HiHat_Close   | 2G～2N | → | HiHat_HalfOpen       |
| HiHat_Close   | 2O～2V | → | HiHat_Foot           |
| Snare         | 38～3F | → | Snare_OpenRim        |
| Snare         | 3G～3N | → | Snare_ClosedRim      |
| Snare         | 3O～3V | → | Snare_Ghost          |
| Tom1          | 58～5F | → | Tom1_Rim             |
| Tom2          | 68～6F | → | Tom2_Rim             |
| Tom3          | 78～7F | → | Tom3_Rim             |
| SE2           | 88～8F | → | RightCymbal Mute     |
| Ride          | 98～9F | → | Ride_Cup             |
| RightCrash    | A0～A7 | → | China                |
| LeftCrash     | B0～B7 | → | Splash               |

