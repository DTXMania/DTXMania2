namespace DTXMania2.オプション設定
{
    partial class 曲読み込みフォルダ割り当てダイアログ
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose( bool disposing )
        {
            if( disposing && ( components != null ) )
            {
                components.Dispose();
            }
            base.Dispose( disposing );
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.buttonOk = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.listViewフォルダ一覧 = new System.Windows.Forms.ListView();
            this.columnHeaderフォルダ名 = ( (System.Windows.Forms.ColumnHeader) ( new System.Windows.Forms.ColumnHeader() ) );
            this.label1 = new System.Windows.Forms.Label();
            this.button選択 = new System.Windows.Forms.Button();
            this.button削除 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // buttonOk
            // 
            this.buttonOk.Anchor = ( (System.Windows.Forms.AnchorStyles) ( ( System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right ) ) );
            this.buttonOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonOk.Font = new System.Drawing.Font( "MS UI Gothic", 12F );
            this.buttonOk.Location = new System.Drawing.Point( 450, 341 );
            this.buttonOk.Name = "buttonOk";
            this.buttonOk.Size = new System.Drawing.Size( 129, 38 );
            this.buttonOk.TabIndex = 5;
            this.buttonOk.Text = "OK";
            this.buttonOk.UseVisualStyleBackColor = true;
            // 
            // buttonCancel
            // 
            this.buttonCancel.Anchor = ( (System.Windows.Forms.AnchorStyles) ( ( System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right ) ) );
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Font = new System.Drawing.Font( "MS UI Gothic", 12F );
            this.buttonCancel.Location = new System.Drawing.Point( 594, 341 );
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size( 129, 38 );
            this.buttonCancel.TabIndex = 6;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            // 
            // listViewフォルダ一覧
            // 
            this.listViewフォルダ一覧.AllowColumnReorder = true;
            this.listViewフォルダ一覧.Columns.AddRange( new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderフォルダ名} );
            this.listViewフォルダ一覧.Font = new System.Drawing.Font( "MS UI Gothic", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ( (byte) ( 128 ) ) );
            this.listViewフォルダ一覧.GridLines = true;
            this.listViewフォルダ一覧.HideSelection = false;
            this.listViewフォルダ一覧.Location = new System.Drawing.Point( 12, 43 );
            this.listViewフォルダ一覧.Name = "listViewフォルダ一覧";
            this.listViewフォルダ一覧.Size = new System.Drawing.Size( 711, 217 );
            this.listViewフォルダ一覧.TabIndex = 7;
            this.listViewフォルダ一覧.UseCompatibleStateImageBehavior = false;
            this.listViewフォルダ一覧.View = System.Windows.Forms.View.Details;
            this.listViewフォルダ一覧.SelectedIndexChanged += new System.EventHandler( this.listViewフォルダ一覧_SelectedIndexChanged );
            // 
            // columnHeaderフォルダ名
            // 
            this.columnHeaderフォルダ名.Text = "フォルダ名";
            this.columnHeaderフォルダ名.Width = 662;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font( "MS UI Gothic", 12F );
            this.label1.Location = new System.Drawing.Point( 11, 9 );
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size( 163, 16 );
            this.label1.TabIndex = 8;
            this.label1.Text = "曲読み込みフォルダ一覧";
            // 
            // button選択
            // 
            this.button選択.Font = new System.Drawing.Font( "MS UI Gothic", 12F );
            this.button選択.Location = new System.Drawing.Point( 12, 277 );
            this.button選択.Name = "button選択";
            this.button選択.Size = new System.Drawing.Size( 129, 38 );
            this.button選択.TabIndex = 9;
            this.button選択.Text = "選択...";
            this.button選択.UseVisualStyleBackColor = true;
            this.button選択.Click += new System.EventHandler( this.button選択_Click );
            // 
            // button削除
            // 
            this.button削除.Enabled = false;
            this.button削除.Font = new System.Drawing.Font( "MS UI Gothic", 12F );
            this.button削除.Location = new System.Drawing.Point( 147, 277 );
            this.button削除.Name = "button削除";
            this.button削除.Size = new System.Drawing.Size( 129, 38 );
            this.button削除.TabIndex = 10;
            this.button削除.Text = "削除";
            this.button削除.UseVisualStyleBackColor = true;
            this.button削除.Click += new System.EventHandler( this.button削除_Click );
            // 
            // 曲読み込みフォルダ割り当てダイアログ
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 12F );
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.buttonCancel;
            this.ClientSize = new System.Drawing.Size( 735, 391 );
            this.Controls.Add( this.button削除 );
            this.Controls.Add( this.button選択 );
            this.Controls.Add( this.label1 );
            this.Controls.Add( this.listViewフォルダ一覧 );
            this.Controls.Add( this.buttonCancel );
            this.Controls.Add( this.buttonOk );
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "曲読み込みフォルダ割り当てダイアログ";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "曲読み込みフォルダ";
            this.TopMost = true;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler( this.曲読み込みフォルダ割り当てダイアログ_FormClosing );
            this.ResumeLayout( false );
            this.PerformLayout();
        }

        #endregion
        private System.Windows.Forms.Button buttonOk;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.ListView listViewフォルダ一覧;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ColumnHeader columnHeaderフォルダ名;
        private System.Windows.Forms.Button button選択;
        private System.Windows.Forms.Button button削除;
    }
}