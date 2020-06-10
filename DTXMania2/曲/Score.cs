using System;
using System.Collections.Generic;
using System.Diagnostics;
using FDK;
using DTXMania2.演奏;

namespace DTXMania2.曲
{
    /// <summary>
    ///		譜面を表すクラス。
    /// </summary>
    class Score : IDisposable
    {

        // プロパティ


        /// <summary>
        ///     難易度レベルに対応する難易度文字列。
        /// </summary>
        public string 難易度ラベル
        {
            get { lock( this._排他 ) return this._難易度ラベル; }
            set { lock( this._排他 ) this._難易度ラベル = value; }
        }

        /// <summary>
        ///     譜面の情報。
        /// </summary>
        /// <remarks>
        ///     <see cref="Score.譜面と画像を現行化済み"/>が false の場合、このプロパティと ScoreDB の対応レコードはまだ現行化されておらず、
        ///     値は<see cref="ScoreDBRecord"/>の既定値（<see cref="ScoreDBRecord.ScorePath"/>と<see cref="ScoreDBRecord.Title"/>のみ設定済み；
        ///     ただし<see cref="ScoreDBRecord.Title"/>は仮の値）である。
        ///     <see cref="Score.譜面と画像を現行化済み"/>が true の場合、このプロパティと ScoreDB の対応レコードは現行化済みである。
        /// </remarks>
        public ScoreDBRecord 譜面
        {
            get { lock( this._排他 ) return this._譜面; }
            set { lock( this._排他 ) this._譜面 = value; }
        }

        /// <summary>
        ///     譜面の属性情報。
        /// </summary>
        /// <remarks>
        ///     <see cref="Score.譜面の属性を現行化済み"/>が false の場合、このプロパティはまだ現行化されておらず、値は null である。
        ///     <see cref="Score.譜面の属性を現行化済み"/>が true の場合、このプロパティは現行化済みであり、値は、ScorePropertiesDB を反映したもの、あるいは null（ScorePropertiesDB 未記載）である。
        /// </remarks>
        public ScorePropertiesDBRecord? 譜面の属性
        {
            get { lock( this._排他 ) return this._譜面の属性; }
            set { lock( this._排他 ) this._譜面の属性 = value; }
        }

        /// <summary>
        ///     この譜面の最高記録情報。
        /// </summary>
        /// <remarks>
        ///     <see cref="Score.最高記録を現行化済み"/>が false の場合、このプロパティはまだ現行化されておらず、値は null である。
        ///     <see cref="Score.最高記録を現行化済み"/>が true の場合、このプロパティは現行化済みであり、値は、RecordDB を反映したもの、あるいは null（RecordDB 未記載）である。
        /// </remarks>
        public RecordDBRecord? 最高記録
        {
            get { lock( this._排他 ) return this._最高記録; }
            set { lock( this._排他 ) this._最高記録 = value; }
        }

        /// <summary>
        ///     <see cref="Score.譜面"/>と画像が現行化済みであれば true。
        /// </summary>
        /// <remarks>
        ///     現行化とは、譜面ファイルと ScoreDB のレコードとの同期を取る作業である。
        ///     レコードが存在していない場合は、新規に追加される。
        ///     譜面ファイルに更新があれば、ScoreDB の対応レコードが更新される。
        /// </remarks>
        public bool 譜面と画像を現行化済み
        {
            get { lock( this._排他 ) return this._譜面と画像を現行化済み; }
            set { lock( this._排他 ) this._譜面と画像を現行化済み = value; }
        }

        /// <summary>
        ///     <see cref="Score.譜面の属性"/>が現行化済みであれば true。
        /// </summary>
        /// <remarks>
        ///     現行化とは、ScorePropertiesDB から曲属性を検索し、該当レコードがあればそれを<see cref="Score.譜面の属性"/>に上書きする作業を表す。
        ///     結果、レコードがあろうがなかろうが、このメンバは true になる。
        /// </remarks>
        public bool 譜面の属性を現行化済み
        {
            get { lock( this._排他 ) return this._譜面の属性を現行化済み; }
            set { lock( this._排他 ) this._譜面の属性を現行化済み = value; }
        }

        /// <summary>
        ///     <see cref="Score.最高記録"/>が現行化済みであれば true。
        /// </summary>
        /// <remarks>
        ///     現行化とは、RecordDB から最高記録を検索し、該当レコードがあればそれを<see cref="Score.最高記録"/>に上書きする作業を表す。
        ///     結果、レコードがあろうがなかろうが、このメンバは true になる。
        /// </remarks>
        public bool 最高記録を現行化済み
        {
            get { lock( this._排他 ) return this._最高記録を現行化済み; }
            set { lock( this._排他 ) this._最高記録を現行化済み = value; }
        }

        /// <summary>
        ///     これまでの最高ランク。
        ///     未取得なら null。
        /// </summary>
        public 結果.ランク種別? 最高ランク
        {
            get
            {
                lock( this._排他 )
                {
                    return ( null != this.最高記録 ) ? 成績.ランクを算出する( this.最高記録.Achievement ) : (結果.ランク種別?) null;
                }
            }
        }

        /// <summary>
        ///     この譜面に存在するノートの、レーン別の総数。
        ///     現行化されていなければ null。
        /// </summary>
        public IReadOnlyDictionary<表示レーン種別, int>? レーン別ノート数
        {
            get
            {
                if( this.譜面と画像を現行化済み && this._レーン別ノート数 is null )
                {
                    #region " 現行化済みかつ未生成ならここで作成する。"
                    //----------------
                    if( this._レーン別ノート数 is null )
                    {
                        this._レーン別ノート数 = new Dictionary<表示レーン種別, int>() {
                            [ 表示レーン種別.LeftCymbal ] = this.譜面.TotalNotes_LeftCymbal,
                            [ 表示レーン種別.HiHat ] = this.譜面.TotalNotes_HiHat,
                            [ 表示レーン種別.Foot ] = this.譜面.TotalNotes_LeftPedal,
                            [ 表示レーン種別.Snare ] = this.譜面.TotalNotes_Snare,
                            [ 表示レーン種別.Bass ] = this.譜面.TotalNotes_Bass,
                            [ 表示レーン種別.Tom1 ] = this.譜面.TotalNotes_HighTom,
                            [ 表示レーン種別.Tom2 ] = this.譜面.TotalNotes_LowTom,
                            [ 表示レーン種別.Tom3 ] = this.譜面.TotalNotes_FloorTom,
                            [ 表示レーン種別.RightCymbal ] = this.譜面.TotalNotes_RightCymbal,
                        };
                    }
                    //----------------
                    #endregion
                }

                return this._レーン別ノート数;
            }
        }

        /// <summary>
        ///     <see cref="ScoreDBRecord.PreImage"/> に対応する、譜面のプレビュー画像。
        /// </summary>
        public 画像D2D? プレビュー画像
        {
            get { lock( this._排他 ) return this._プレビュー画像; }
            set { lock( this._排他 ) this._プレビュー画像 = value; }
        }

        /// <summary>
        ///     <see cref="ScoreDBRecord.Title"/> を画像化したもの。
        /// </summary>
        public 文字列画像D2D? タイトル文字列画像
        {
            get { lock( this._排他 ) return this._タイトル文字列画像; }
            set { lock( this._排他 ) this._タイトル文字列画像 = value; }
        }

        /// <summary>
        ///     <see cref="ScoreDBRecord.Artist"/> を画像化したもの。
        /// </summary>
        public 文字列画像D2D? サブタイトル文字列画像
        {
            get { lock( this._排他 ) return this._サブタイトル文字列画像; }
            set { lock( this._排他 ) this._サブタイトル文字列画像 = value; }
        }



        // 生成と終了


        public Score()
        {
        }

        public virtual void Dispose()
        {
            this.プレビュー画像?.Dispose();
            this.タイトル文字列画像?.Dispose();
            this.サブタイトル文字列画像?.Dispose();
        }



        // ローカル


        private string _難易度ラベル = "FREE";

        private ScoreDBRecord _譜面 = new ScoreDBRecord();

        private ScorePropertiesDBRecord? _譜面の属性 = null;

        private RecordDBRecord? _最高記録 = null;

        private bool _譜面と画像を現行化済み = false;

        private bool _譜面の属性を現行化済み = false;

        private bool _最高記録を現行化済み = false;

        private Dictionary<表示レーン種別, int> _レーン別ノート数 = null!;

        private 画像D2D? _プレビュー画像 = null;

        private 文字列画像D2D? _タイトル文字列画像 = null;

        private 文字列画像D2D? _サブタイトル文字列画像 = null;

        /// <summary>
        ///     現行化処理との排他用。
        /// </summary>
        private readonly object _排他 = new object();
    }
}
