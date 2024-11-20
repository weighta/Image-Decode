using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DXT3_to_text
{
    public partial class ImageForm : Form
    {
        public ImageForm(PictureBox pb, int width, int height)
        {

            InitializeComponent();
            Width = width + 16;
            Height = height + 39;
            pictureBox1.Image = pb.Image;
        }

        private void ImageForm_Load(object sender, EventArgs e)
        {

        }
    }
}
