using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace SSTFEditor
{
    public partial class Popupメッセージ : Form
    {
        public Popupメッセージ( string strメッセージ )
        {
            InitializeComponent();

            this.メッセージ = strメッセージ;
            this.フォント = new Font( "MS PGothic", 10f );
        }


        protected string メッセージ;

        protected Font フォント;


        protected void Popupメッセージ_FormClosing( object sender, FormClosingEventArgs e )
        {
            this.フォント?.Dispose();
            this.フォント = null;
        }

        protected void Popupメッセージ_Load( object sender, EventArgs e )
        {
            base.Location = new Point(
                base.Owner.Location.X + ( ( base.Owner.Width - base.Width ) / 2 ),
                base.Owner.Location.Y + ( ( base.Owner.Height - base.Height ) / 2 ) );
        }

        protected void panelメッセージ本文_Paint( object sender, PaintEventArgs e )
        {
            using( var brush = new SolidBrush( Color.Black ) )
            {
                e.Graphics.DrawString(
                    this.メッセージ,
                    this.フォント,
                    brush,
                    new RectangleF( 0f, 0f, (float) this.panelメッセージ本文.ClientSize.Width, (float) this.panelメッセージ本文.ClientSize.Height ),
                    new StringFormat() {
                        LineAlignment = StringAlignment.Center,
                        Alignment = StringAlignment.Near,
                    } );
            }
        }
    }
}
