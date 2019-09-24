using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using FDK;
using SSTFormat.v4;

using Song = DTXMania.Song06;
using Record = DTXMania.Record08;

namespace DTXMania
{
    /// <summary>
    ///		曲ツリー階層において「曲」を表すノード。
    /// </summary>
    class MusicNode : Node
    {
        /// <summary>
        ///		この曲ノードに対応する曲ファイル。
        ///		絶対パス。
        /// </summary>
        public VariablePath 曲ファイルの絶対パス { get; set; } = null;

        /// <summary>
        ///		この曲ノードに対応する曲ファイルのハッシュ値。
        /// </summary>
        public string 曲ファイルのハッシュ { get; set; } = null;

        /// <summary>
        ///     この曲ノードに対応する曲ファイルの最終更新日時。
        ///		文字列の書式は、System.DateTime.ToString("G") と同じ。（例: "08/17/2000 16:32:32"）
        ///		カルチャはシステム既定のものとする。
        /// </summary>
        public string 曲ファイルの最終更新日時 { get; set; } = null;

        /// <summary>
        ///     この曲のBGMの再生タイミングを、この時間[ms]分だけ前後にずらす。（負数で早める、正数で遅める）
        /// </summary>
        public int BGMAdjust { get; set; } = 0;

        public double 最小BPM { get; set; } = 0.0;

        public double 最大BPM { get; set; } = 0.0;

        public VariablePath プレビュー画像ファイルの絶対パス { get; set; } = null;

        public Dictionary<表示レーン種別, int> レーン別ノート数 { get; set; } = null;

        /// <summary>
        ///     これまでの最高達成率。0～100。
        ///     未設定なら null。
        /// </summary>
        public double? 達成率 { get; set; } = null;

        /// <summary>
        ///     これまでの最高ランク。
        ///     未設定なら null。
        /// </summary>
        public ランク種別? ランク => ( this.達成率.HasValue ) ? 成績.ランクを算出する( this.達成率.Value ) : (ランク種別?) null;



        // 生成と終了


        public MusicNode( VariablePath 曲ファイルの絶対パス, Node 親ノード = null )
        {
            this.親ノード = 親ノード;
            this.曲ファイルの絶対パス = 曲ファイルの絶対パス;
            this.ノード画像 = Node.現行化前のノード画像;

            this.タイトル = "(New song!)";
            this.サブタイトル = "Now Loading...";
        }

        public override void Dispose()
        {
            this.ノード画像?.Dispose();

            base.Dispose();
        }


        
        // 現行化


        public bool 現行化未実施 { get; set; } = true;

        public bool 現行化済み
        {
            get => !this.現行化未実施;
            set => this.現行化未実施 = !value;
        }

        public readonly object 現行化処理の排他 = new object();

        public void 現行化する( SongDB songdb, RecordDB recorddb )
        {
            if( this.現行化済み || AppForm.ビュアーモードである )
                return;

            lock( this.現行化処理の排他 )
            {
                this.現行化済み = true;  // 先に設定

                #region " SongDB と同期する。"
                //----------------
                try
                {
                    var 同一パス曲 = songdb.Songs.Where( ( song ) => ( song.Path == this.曲ファイルの絶対パス.変数なしパス ) ).SingleOrDefault();

                    if( null == 同一パス曲 )
                    {
                        // (A) 同一パスを持つレコードがDBになかった

                        // スコアを読み込む。
                        var score = スコア.ファイルから生成する( this.曲ファイルの絶対パス.変数なしパス );   // ノーツ数やBPMを算出するため、ヘッダだけじゃなくすべてを読み込む。

                        var ノーツ数 = _ノーツ数を算出して返す( score, App進行描画.ユーザ管理.ログオン中のユーザ );
                        var BPMs = _最小最大BPMを調べて返す( score );
                        var 最終更新日時 = File.GetLastWriteTime( this.曲ファイルの絶対パス.変数なしパス ).ToString( "G" );
                        var ハッシュ = _ファイルのハッシュを算出して返す( this.曲ファイルの絶対パス );

                        var 同一ハッシュ曲 = songdb.Songs.Where( ( song ) => ( song.HashId == ハッシュ ) ).SingleOrDefault();

                        if( null == 同一ハッシュ曲 )
                        {
                            #region " (A-a) 同一ハッシュを持つレコードもDBになかった → 新規追加 "
                            //----------------
                            // スコアをもとに、SongDB にレコードを新規追加する。
                            var song = new Song() {
                                HashId = ハッシュ,
                                Title = score.曲名,
                                Path = this.曲ファイルの絶対パス.変数なしパス,
                                LastWriteTime = 最終更新日時,
                                Level = score.難易度,
                                MinBPM = BPMs.最小BPM,
                                MaxBPM = BPMs.最大BPM,
                                TotalNotes_LeftCymbal = ノーツ数[ 表示レーン種別.LeftCymbal ],
                                TotalNotes_HiHat = ノーツ数[ 表示レーン種別.HiHat ],
                                TotalNotes_LeftPedal = ノーツ数[ 表示レーン種別.Foot ],
                                TotalNotes_Snare = ノーツ数[ 表示レーン種別.Snare ],
                                TotalNotes_Bass = ノーツ数[ 表示レーン種別.Bass ],
                                TotalNotes_HighTom = ノーツ数[ 表示レーン種別.Tom1 ],
                                TotalNotes_LowTom = ノーツ数[ 表示レーン種別.Tom2 ],
                                TotalNotes_FloorTom = ノーツ数[ 表示レーン種別.Tom3 ],
                                TotalNotes_RightCymbal = ノーツ数[ 表示レーン種別.RightCymbal ],
                                PreImage = ( score.プレビュー画像ファイル名.Nullでも空でもない() ) ? score.プレビュー画像ファイル名 : "",  // 相対パス
                                Artist = score.アーティスト名,
                                PreSound = ( score.プレビュー音声ファイル名.Nullでも空でもない() ) ? score.プレビュー音声ファイル名 : "",  // 相対パス
                                BGMAdjust = 0,
                            };
                            songdb.Songs.InsertOnSubmit( song );

                            // スコアを、自身（MusinNode）にも反映する。
                            this.レコードを反映する( song );

                            Log.Info( $"DBに曲を追加しました。{this.曲ファイルの絶対パス.変数付きパス}" );
                            //----------------
                            #endregion
                        }
                        else
                        {
                            #region " (A-b) 同一ハッシュを持つレコードがDBにあった → 更新 "
                            //----------------
                            // スコアをもとに、SongDB のレコードを更新する。
                            //同一ハッシュ曲.HashId → 更新不要
                            同一ハッシュ曲.Title = score.曲名;
                            同一ハッシュ曲.Path = this.曲ファイルの絶対パス.変数なしパス;
                            同一ハッシュ曲.LastWriteTime = 最終更新日時;
                            同一ハッシュ曲.Level = score.難易度;
                            同一ハッシュ曲.MinBPM = BPMs.最小BPM;
                            同一ハッシュ曲.MaxBPM = BPMs.最大BPM;
                            同一ハッシュ曲.TotalNotes_LeftCymbal = ノーツ数[ 表示レーン種別.LeftCymbal ];
                            同一ハッシュ曲.TotalNotes_HiHat = ノーツ数[ 表示レーン種別.HiHat ];
                            同一ハッシュ曲.TotalNotes_LeftPedal = ノーツ数[ 表示レーン種別.Foot ];
                            同一ハッシュ曲.TotalNotes_Snare = ノーツ数[ 表示レーン種別.Snare ];
                            同一ハッシュ曲.TotalNotes_Bass = ノーツ数[ 表示レーン種別.Bass ];
                            同一ハッシュ曲.TotalNotes_HighTom = ノーツ数[ 表示レーン種別.Tom1 ];
                            同一ハッシュ曲.TotalNotes_LowTom = ノーツ数[ 表示レーン種別.Tom2 ];
                            同一ハッシュ曲.TotalNotes_FloorTom = ノーツ数[ 表示レーン種別.Tom3 ];
                            同一ハッシュ曲.TotalNotes_RightCymbal = ノーツ数[ 表示レーン種別.RightCymbal ];
                            同一ハッシュ曲.PreImage = ( score.プレビュー画像ファイル名.Nullでも空でもない() ) ? score.プレビュー画像ファイル名 : ""; // 相対パス
                            同一ハッシュ曲.Artist = score.アーティスト名;
                            同一ハッシュ曲.PreSound = ( score.プレビュー音声ファイル名.Nullでも空でもない() ) ? score.プレビュー音声ファイル名 : ""; // 相対パス
                            同一ハッシュ曲.BGMAdjust = 0;  // リセット。

                            // スコアを、自身（MusinNode）にも反映する。
                            this.レコードを反映する( 同一ハッシュ曲 );

                            Log.Info( $"パスが異なりハッシュが同一であるレコードが検出されたため、曲の情報を更新しました。{this.曲ファイルの絶対パス.変数付きパス}" );
                            //----------------
                            #endregion
                        }
                    }
                    else
                    {
                        // (B) 同一パスを持つレコードがDBにあった

                        string 曲ファイルの最終更新日時 = File.GetLastWriteTime( this.曲ファイルの絶対パス.変数なしパス ).ToString( "G" );

                        if( 同一パス曲.LastWriteTime != 曲ファイルの最終更新日時 )
                        {
                            #region " (B-a) 最終更新日時が変更されている → 更新 "
                            //----------------
                            // スコアを読み込む。
                            var score = スコア.ファイルから生成する( this.曲ファイルの絶対パス.変数なしパス );
                            var ハッシュ = _ファイルのハッシュを算出して返す( this.曲ファイルの絶対パス );
                            var ノーツ数 = _ノーツ数を算出して返す( score, App進行描画.ユーザ管理.ログオン中のユーザ );
                            var BPMs = _最小最大BPMを調べて返す( score );

                            // レコードを更新する。
                            //同一パス曲.HashId → 後述
                            同一パス曲.Title = score.曲名;
                            同一パス曲.LastWriteTime = 曲ファイルの最終更新日時;
                            同一パス曲.Level = score.難易度;
                            同一パス曲.MinBPM = BPMs.最小BPM;
                            同一パス曲.MaxBPM = BPMs.最大BPM;
                            同一パス曲.TotalNotes_LeftCymbal = ノーツ数[ 表示レーン種別.LeftCymbal ];
                            同一パス曲.TotalNotes_HiHat = ノーツ数[ 表示レーン種別.HiHat ];
                            同一パス曲.TotalNotes_LeftPedal = ノーツ数[ 表示レーン種別.Foot ];
                            同一パス曲.TotalNotes_Snare = ノーツ数[ 表示レーン種別.Snare ];
                            同一パス曲.TotalNotes_Bass = ノーツ数[ 表示レーン種別.Bass ];
                            同一パス曲.TotalNotes_HighTom = ノーツ数[ 表示レーン種別.Tom1 ];
                            同一パス曲.TotalNotes_LowTom = ノーツ数[ 表示レーン種別.Tom2 ];
                            同一パス曲.TotalNotes_FloorTom = ノーツ数[ 表示レーン種別.Tom3 ];
                            同一パス曲.TotalNotes_RightCymbal = ノーツ数[ 表示レーン種別.RightCymbal ];
                            同一パス曲.PreImage = ( score.プレビュー画像ファイル名.Nullでも空でもない() ) ? score.プレビュー画像ファイル名 : "";
                            同一パス曲.Artist = score.アーティスト名;
                            同一パス曲.PreSound = ( score.プレビュー音声ファイル名.Nullでも空でもない() ) ? score.プレビュー音声ファイル名 : "";
                            同一パス曲.BGMAdjust = 0;

                            var newRecord = 同一パス曲.Clone();
                            if( ハッシュ != 同一パス曲.HashId )
                            {
                                // ハッシュはキーなので、これが変わったら、古いレコードを削除して、新しいレコードを追加する。
                                songdb.Songs.DeleteOnSubmit( 同一パス曲 );
                                songdb.DataContext.SubmitChanges(); // 一度Submitして先にレコード削除を確定しないと、次のInsertがエラーになる。（PathカラムはUnique属性なので）

                                newRecord.HashId = ハッシュ;
                                songdb.Songs.InsertOnSubmit( newRecord );
                            }

                            // レコードを、自身（MusinNode）にも反映する。
                            this.レコードを反映する( newRecord );

                            Log.Info( $"最終更新日時が変更されているため、曲の情報を更新しました。{this.曲ファイルの絶対パス.変数付きパス}" );
                            //----------------
                            #endregion
                        }
                        else
                        {
                            #region " (B-b) それ以外 → 何もしない "
                            //----------------
                            // レコードを、自身（MusinNode）にも反映する。
                            this.レコードを反映する( 同一パス曲 );
                            //----------------
                            #endregion
                        }
                    }
                }
                catch( Exception e )
                {
                    Log.ERROR( $"曲DBへの曲の追加に失敗しました。({VariablePath.絶対パスをフォルダ変数付き絶対パスに変換して返す( e.Message )})[{this.曲ファイルの絶対パス.変数付きパス}]" );
                    return;
                }
                //----------------
                #endregion

                #region " ノード画像を生成する。"
                //----------------
                {
                    if( this.ノード画像 != Node.既定のノード画像 && this.ノード画像 != Node.現行化前のノード画像 )
                        this.ノード画像?.Dispose();

                    this.ノード画像 = null;

                    if( this.プレビュー画像ファイルの絶対パス?.変数なしパス.Nullでも空でもない() ?? false )
                        this.ノード画像 = new テクスチャ( this.プレビュー画像ファイルの絶対パス.変数なしパス );
                }
                //----------------
                #endregion

                #region " 成績があれば取得する。"
                //----------------
                try
                {
                    var record = recorddb?.Records.Where( ( r ) => ( 
                        r.UserId == App進行描画.ユーザ管理.ログオン中のユーザ.ユーザID && 
                        r.SongHashId == this.曲ファイルのハッシュ ) ).SingleOrDefault();

                    if( null != record )
                    {
                        // あれば、成績を自身（MusicNode）に反映する。
                        this.達成率 = record.Achievement;
                    }
                    else
                    {
                        // なければ、リセット。
                        this.達成率 = null;
                    }
                }
                catch( Exception e )
                {
                    Log.ERROR( $"成績DBの取得に失敗しました。({VariablePath.絶対パスをフォルダ変数付き絶対パスに変換して返す( e.Message )})[{this.曲ファイルの絶対パス.変数付きパス}]" );
                    this.達成率 = null;
                }
                //----------------
                #endregion
            }
        }

        public void レコードを反映する( Song song )
        {
            var 曲ファイルのあるフォルダ = Path.GetDirectoryName( this.曲ファイルの絶対パス.変数なしパス ) ?? "";  // Path.GetDirectoryName は、ルートディレクトリの場合 null が返される。

            this.タイトル = song.Title;
            this.サブタイトル = song.Artist;
            this.プレビュー画像ファイルの絶対パス = ( string.IsNullOrEmpty( song.PreImage ) ) ? null : Path.Combine( 曲ファイルのあるフォルダ, song.PreImage );
            this.プレビュー音声ファイルの絶対パス = ( string.IsNullOrEmpty( song.PreSound ) ) ? null : Path.Combine( 曲ファイルのあるフォルダ, song.PreSound );
            this.レーン別ノート数 = new Dictionary<表示レーン種別, int>() {
                [ 表示レーン種別.LeftCymbal ] = song.TotalNotes_LeftCymbal,
                [ 表示レーン種別.HiHat ] = song.TotalNotes_HiHat,
                [ 表示レーン種別.Foot ] = song.TotalNotes_LeftPedal,
                [ 表示レーン種別.Snare ] = song.TotalNotes_Snare,
                [ 表示レーン種別.Bass ] = song.TotalNotes_Bass,
                [ 表示レーン種別.Tom1 ] = song.TotalNotes_HighTom,
                [ 表示レーン種別.Tom2 ] = song.TotalNotes_LowTom,
                [ 表示レーン種別.Tom3 ] = song.TotalNotes_FloorTom,
                [ 表示レーン種別.RightCymbal ] = song.TotalNotes_RightCymbal,
            };
            this.曲ファイルのハッシュ = song.HashId;
            this.曲ファイルの最終更新日時 = song.LastWriteTime;
            this.最小BPM = song.MinBPM ?? 0.0;
            this.最大BPM = song.MaxBPM ?? 0.0;
            this.BGMAdjust = song.BGMAdjust;
            this.難易度 = song.Level;
            this.難易度ラベル = ( this.親ノード is SetNode setnode ) ? ( setnode.MusicNodes.First( ( m ) => m == this ).難易度ラベル ) : "FREE";
        }



        // private(static)


        private static string _ファイルのハッシュを算出して返す( VariablePath 曲ファイルの絶対パス )
        {
            var sha512 = new SHA512CryptoServiceProvider();
            byte[] hash = null;

            try
            {
                using( var fs = new FileStream( 曲ファイルの絶対パス.変数なしパス, FileMode.Open, FileAccess.Read ) )
                    hash = sha512.ComputeHash( fs );

                var hashString = new StringBuilder();
                foreach( byte b in hash )
                    hashString.Append( b.ToString( "X2" ) );

                return hashString.ToString();
            }
            catch( Exception e )
            {
                Log.ERROR( $"ファイルからのハッシュの作成に失敗しました。({e})[{曲ファイルの絶対パス.変数付きパス}]" );
                throw;
            }
        }

        private static Dictionary<表示レーン種別, int> _ノーツ数を算出して返す( スコア score, ユーザ設定 ユーザ設定 )
        {
            var ノーツ数 = new Dictionary<表示レーン種別, int>();

            foreach( 表示レーン種別 lane in Enum.GetValues( typeof( 表示レーン種別 ) ) )
                ノーツ数.Add( lane, 0 );

            foreach( var chip in score.チップリスト )
            {
                var ドラムチッププロパティ = ユーザ設定.ドラムチッププロパティ管理[ chip.チップ種別 ];

                // AutoPlay ON のチップは、すべてがONである場合を除いて、カウントしない。
                if( ユーザ設定.AutoPlay[ ドラムチッププロパティ.AutoPlay種別 ] )
                {
                    if( !( ユーザ設定.AutoPlayがすべてONである ) )
                        continue;
                }
                // AutoPlay OFF 時でもユーザヒットの対象にならないチップはカウントしない。
                if( !( ドラムチッププロパティ.AutoPlayOFF_ユーザヒット ) )
                    continue;

                ノーツ数[ ドラムチッププロパティ.表示レーン種別 ]++;
            }

            return ノーツ数;
        }

        private static (double 最小BPM, double 最大BPM) _最小最大BPMを調べて返す( スコア score )
        {
            var result = (最小BPM: double.MaxValue, 最大BPM: double.MinValue);

            var BPMchips = score.チップリスト.Where( ( c ) => ( c.チップ種別 == チップ種別.BPM ) );
            foreach( var chip in BPMchips )
            {
                result.最小BPM = Math.Min( result.最小BPM, chip.BPM );
                result.最大BPM = Math.Max( result.最大BPM, chip.BPM );
            }

            if( result.最小BPM == double.MaxValue || result.最大BPM == double.MinValue )    // BPMチップがひとつもなかった
            {
                double 初期BPM = スコア.初期BPM;
                result = (初期BPM, 初期BPM);
            }

            return result;
        }
    }
}
