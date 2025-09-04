using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

public class RoundedRichTextBox : RichTextBox
{
    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, ref RECT lParam);

    private const int WM_PAINT = 0x000F;
    private const int WM_SIZE = 0x0005;
    private const int WM_THEMECHANGED = 0x031A;
    private const int EM_SETRECT = 0x00B3;

    private Color _borderColor = Color.DodgerBlue;
    private int _borderRadius = 5;
    private int _borderSize = 2;
    private int _innerPadding = 4;

    [Category("Appearance")]
    [DefaultValue(typeof(Color), "DodgerBlue")]
    public Color BorderColor
    {
        get => _borderColor;
        set { _borderColor = value; Invalidate(); }
    }

    [Category("Appearance")]
    [DefaultValue(5)]
    public int BorderRadius
    {
        get => _borderRadius;
        set { _borderRadius = Math.Max(0, value); UpdateRoundedRegion(); Invalidate(); }
    }

    [Category("Appearance")]
    [DefaultValue(2)]
    public int BorderSize
    {
        get => _borderSize;
        set { _borderSize = Math.Max(1, value); UpdateFormatRect(); Invalidate(); }
    }

    [Category("Layout")]
    [DefaultValue(4)]
    public int InnerPadding
    {
        get => _innerPadding;
        set { _innerPadding = Math.Max(0, value); UpdateFormatRect(); Invalidate(); }
    }

    public RoundedRichTextBox()
    {
        BorderStyle = BorderStyle.None;
        SetStyle(ControlStyles.AllPaintingInWmPaint, true);
        SetStyle(ControlStyles.OptimizedDoubleBuffer, true);

        Resize += (_, __) => { UpdateRoundedRegion(); UpdateFormatRect(); };
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            const int WS_BORDER = 0x00800000;
            const int WS_EX_CLIENTEDGE = 0x00000200;
            cp.Style &= ~WS_BORDER;
            cp.ExStyle &= ~WS_EX_CLIENTEDGE;
            return cp;
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        UpdateRoundedRegion();
        UpdateFormatRect();
        Invalidate();
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);

        if (m.Msg == WM_PAINT || m.Msg == WM_SIZE || m.Msg == WM_THEMECHANGED)
        {
            using (var g = Graphics.FromHwnd(Handle))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                DrawBorder(g);
            }
        }
    }

    private void DrawBorder(Graphics g)
    {
        var r = ClientRectangle;
        if (r.Width <= 0 || r.Height <= 0) return;

        r.Inflate(-1, -1);

        using (var path = GetRoundedPath(r, _borderRadius))
        using (var pen = new Pen(_borderColor, _borderSize) { Alignment = PenAlignment.Inset })
        {
            g.DrawPath(pen, path);
        }
    }

    private void UpdateRoundedRegion()
    {
        if (!IsHandleCreated) return;
        var r = ClientRectangle;
        if (r.Width <= 0 || r.Height <= 0) return;

        using (var path = GetRoundedPath(r, _borderRadius))
        {
            Region = new Region(path);
        }
    }

    private void UpdateFormatRect()
    {
        if (!IsHandleCreated) return;

        int pad = Math.Max(_innerPadding, _borderSize + 1);

        RECT rc = new RECT
        {
            Left = pad,
            Top = pad,
            Right = ClientSize.Width - pad,
            Bottom = ClientSize.Height - pad
        };

        SendMessage(Handle, EM_SETRECT, IntPtr.Zero, ref rc);
    }


    private static GraphicsPath GetRoundedPath(Rectangle rect, int radius)
    {
        int d = radius * 2;
        var path = new GraphicsPath();

        if (radius <= 0)
        {
            path.AddRectangle(rect);
            return path;
        }

        Rectangle arc = new Rectangle(rect.Location, new Size(d, d));

        path.AddArc(arc, 180, 90);
        arc.X = rect.Right - d;
        path.AddArc(arc, 270, 90);
        arc.Y = rect.Bottom - d;
        path.AddArc(arc, 0, 90);
        arc.X = rect.Left;
        path.AddArc(arc, 90, 90);

        path.CloseFigure();
        return path;
    }
}