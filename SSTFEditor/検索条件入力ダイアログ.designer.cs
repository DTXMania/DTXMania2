namespace SSTFEditor
{
	partial class 検索条件入力ダイアログ
	{
		/// <summary>
		/// 必要なデザイナ変数です。
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// 使用中のリソースをすべてクリーンアップします。
		/// </summary>
		/// <param name="disposing">マネージ リソースが破棄される場合 true、破棄されない場合は false です。</param>
		protected override void Dispose( bool disposing )
		{
			if( disposing && ( components != null ) )
			{
				components.Dispose();
			}
			base.Dispose( disposing );
		}

		#region Windows フォーム デザイナで生成されたコード

		/// <summary>
		/// デザイナ サポートに必要なメソッドです。このメソッドの内容を
		/// コード エディタで変更しないでください。
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager( typeof( 検索条件入力ダイアログ ) );
			this.toolTip1 = new System.Windows.Forms.ToolTip( this.components );
			this.checkBox小節範囲指定 = new System.Windows.Forms.CheckBox();
			this.buttonOK = new System.Windows.Forms.Button();
			this.buttonキャンセル = new System.Windows.Forms.Button();
			this.textBox小節範囲終了 = new System.Windows.Forms.TextBox();
			this.textBox小節範囲開始 = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.buttonAllレーン = new System.Windows.Forms.Button();
			this.buttonClearレーン = new System.Windows.Forms.Button();
			this.checkedListBoxレーン選択リスト = new System.Windows.Forms.CheckedListBox();
			this.checkedListBoxチップ選択リスト = new System.Windows.Forms.CheckedListBox();
			this.buttonAllチップ = new System.Windows.Forms.Button();
			this.buttonClearチップ = new System.Windows.Forms.Button();
			this.labelレーンから選択 = new System.Windows.Forms.Label();
			this.labelチップから選択 = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// checkBox小節範囲指定
			// 
			resources.ApplyResources( this.checkBox小節範囲指定, "checkBox小節範囲指定" );
			this.checkBox小節範囲指定.Name = "checkBox小節範囲指定";
			this.toolTip1.SetToolTip( this.checkBox小節範囲指定, resources.GetString( "checkBox小節範囲指定.ToolTip" ) );
			this.checkBox小節範囲指定.UseVisualStyleBackColor = true;
			this.checkBox小節範囲指定.CheckStateChanged += new System.EventHandler( this.checkBox小節範囲指定_CheckStateChanged );
			this.checkBox小節範囲指定.KeyDown += new System.Windows.Forms.KeyEventHandler( this.checkBox小節範囲指定_KeyDown );
			// 
			// buttonOK
			// 
			resources.ApplyResources( this.buttonOK, "buttonOK" );
			this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.UseVisualStyleBackColor = true;
			// 
			// buttonキャンセル
			// 
			resources.ApplyResources( this.buttonキャンセル, "buttonキャンセル" );
			this.buttonキャンセル.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonキャンセル.Name = "buttonキャンセル";
			this.buttonキャンセル.UseVisualStyleBackColor = true;
			// 
			// textBox小節範囲終了
			// 
			resources.ApplyResources( this.textBox小節範囲終了, "textBox小節範囲終了" );
			this.textBox小節範囲終了.Name = "textBox小節範囲終了";
			this.textBox小節範囲終了.KeyDown += new System.Windows.Forms.KeyEventHandler( this.textBox小節範囲終了_KeyDown );
			// 
			// textBox小節範囲開始
			// 
			resources.ApplyResources( this.textBox小節範囲開始, "textBox小節範囲開始" );
			this.textBox小節範囲開始.Name = "textBox小節範囲開始";
			this.textBox小節範囲開始.KeyDown += new System.Windows.Forms.KeyEventHandler( this.textBox小節範囲開始_KeyDown );
			// 
			// label2
			// 
			resources.ApplyResources( this.label2, "label2" );
			this.label2.Name = "label2";
			// 
			// buttonAllレーン
			// 
			resources.ApplyResources( this.buttonAllレーン, "buttonAllレーン" );
			this.buttonAllレーン.Name = "buttonAllレーン";
			this.buttonAllレーン.UseVisualStyleBackColor = true;
			this.buttonAllレーン.Click += new System.EventHandler( this.buttonAllレーン_Click );
			this.buttonAllレーン.KeyDown += new System.Windows.Forms.KeyEventHandler( this.buttonAllレーン_KeyDown );
			// 
			// buttonClearレーン
			// 
			resources.ApplyResources( this.buttonClearレーン, "buttonClearレーン" );
			this.buttonClearレーン.Name = "buttonClearレーン";
			this.buttonClearレーン.UseVisualStyleBackColor = true;
			this.buttonClearレーン.Click += new System.EventHandler( this.buttonClearレーン_Click );
			this.buttonClearレーン.KeyDown += new System.Windows.Forms.KeyEventHandler( this.buttonClearレーン_KeyDown );
			// 
			// checkedListBoxレーン選択リスト
			// 
			resources.ApplyResources( this.checkedListBoxレーン選択リスト, "checkedListBoxレーン選択リスト" );
			this.checkedListBoxレーン選択リスト.FormattingEnabled = true;
			this.checkedListBoxレーン選択リスト.Name = "checkedListBoxレーン選択リスト";
			this.checkedListBoxレーン選択リスト.KeyDown += new System.Windows.Forms.KeyEventHandler( this.checkedListBoxレーン選択リスト_KeyDown );
			// 
			// checkedListBoxチップ選択リスト
			// 
			resources.ApplyResources( this.checkedListBoxチップ選択リスト, "checkedListBoxチップ選択リスト" );
			this.checkedListBoxチップ選択リスト.FormattingEnabled = true;
			this.checkedListBoxチップ選択リスト.Name = "checkedListBoxチップ選択リスト";
			// 
			// buttonAllチップ
			// 
			resources.ApplyResources( this.buttonAllチップ, "buttonAllチップ" );
			this.buttonAllチップ.Name = "buttonAllチップ";
			this.buttonAllチップ.UseVisualStyleBackColor = true;
			this.buttonAllチップ.Click += new System.EventHandler( this.buttonAllチップ_Click );
			this.buttonAllチップ.KeyDown += new System.Windows.Forms.KeyEventHandler( this.buttonAllチップ_KeyDown );
			// 
			// buttonClearチップ
			// 
			resources.ApplyResources( this.buttonClearチップ, "buttonClearチップ" );
			this.buttonClearチップ.Name = "buttonClearチップ";
			this.buttonClearチップ.UseVisualStyleBackColor = true;
			this.buttonClearチップ.Click += new System.EventHandler( this.buttonClearチップ_Click );
			this.buttonClearチップ.KeyDown += new System.Windows.Forms.KeyEventHandler( this.buttonClearチップ_KeyDown );
			// 
			// labelレーンから選択
			// 
			resources.ApplyResources( this.labelレーンから選択, "labelレーンから選択" );
			this.labelレーンから選択.Name = "labelレーンから選択";
			// 
			// labelチップから選択
			// 
			resources.ApplyResources( this.labelチップから選択, "labelチップから選択" );
			this.labelチップから選択.Name = "labelチップから選択";
			// 
			// C検索条件入力ダイアログ
			// 
			resources.ApplyResources( this, "$this" );
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ControlBox = false;
			this.Controls.Add( this.labelチップから選択 );
			this.Controls.Add( this.labelレーンから選択 );
			this.Controls.Add( this.buttonAllチップ );
			this.Controls.Add( this.buttonClearチップ );
			this.Controls.Add( this.checkedListBoxチップ選択リスト );
			this.Controls.Add( this.buttonAllレーン );
			this.Controls.Add( this.buttonClearレーン );
			this.Controls.Add( this.checkedListBoxレーン選択リスト );
			this.Controls.Add( this.checkBox小節範囲指定 );
			this.Controls.Add( this.textBox小節範囲終了 );
			this.Controls.Add( this.textBox小節範囲開始 );
			this.Controls.Add( this.label2 );
			this.Controls.Add( this.buttonOK );
			this.Controls.Add( this.buttonキャンセル );
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "C検索条件入力ダイアログ";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler( this.OnFormClosing );
			this.KeyDown += new System.Windows.Forms.KeyEventHandler( this.OnKeyDown );
			this.ResumeLayout( false );
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ToolTip toolTip1;
		private System.Windows.Forms.Button buttonOK;
		private System.Windows.Forms.Button buttonキャンセル;
		private System.Windows.Forms.CheckBox checkBox小節範囲指定;
		private System.Windows.Forms.TextBox textBox小節範囲終了;
		private System.Windows.Forms.TextBox textBox小節範囲開始;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Button buttonAllレーン;
		private System.Windows.Forms.Button buttonClearレーン;
		private System.Windows.Forms.CheckedListBox checkedListBoxレーン選択リスト;
		private System.Windows.Forms.CheckedListBox checkedListBoxチップ選択リスト;
		private System.Windows.Forms.Button buttonAllチップ;
		private System.Windows.Forms.Button buttonClearチップ;
		private System.Windows.Forms.Label labelレーンから選択;
		private System.Windows.Forms.Label labelチップから選択;
	}
}