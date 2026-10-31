using System;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace FocusPulse
{
    public class SurfaceTheme
    {
        public string Key { get; set; }
        public string DisplayTitle { get; set; }
        public bool IsDark { get; set; }
        public string PaletteKey { get; set; }
        public string PaletteName { get; set; }
        public string AccentHex { get; set; }
        public SolidColorBrush CanvasBg { get; set; }
        public SolidColorBrush SurfaceCard { get; set; }
        public SolidColorBrush SurfaceCardHover { get; set; }
        public SolidColorBrush SurfaceElevated { get; set; }
        public SolidColorBrush SurfaceElevatedHover { get; set; }
        public SolidColorBrush SurfaceActiveTab { get; set; }
        public SolidColorBrush HairlineBorder { get; set; }
        public SolidColorBrush SubtleBorder { get; set; }
        public SolidColorBrush TextPrimary { get; set; }
        public SolidColorBrush TextSecondary { get; set; }
        public SolidColorBrush TextMuted { get; set; }
        public SolidColorBrush BrandAccent { get; set; }
        public SolidColorBrush BrandAccentHover { get; set; }

        // Dynamic Badges
        public SolidColorBrush FocusBadgeText { get; set; }
        public SolidColorBrush FocusBadgeBg { get; set; }
        public SolidColorBrush FocusBadgeBorder { get; set; }

        public SolidColorBrush GeneralBadgeText { get; set; }
        public SolidColorBrush GeneralBadgeBg { get; set; }
        public SolidColorBrush GeneralBadgeBorder { get; set; }

        public SolidColorBrush DistractBadgeText { get; set; }
        public SolidColorBrush DistractBadgeBg { get; set; }
        public SolidColorBrush DistractBadgeBorder { get; set; }

        public SolidColorBrush AmberBadgeText { get; set; }
        public SolidColorBrush AmberBadgeBg { get; set; }

        public SolidColorBrush VioletBadgeText { get; set; }
        public SolidColorBrush VioletBadgeBg { get; set; }

        // Swatches for Settings Preview
        public string SwatchCanvas { get; set; }
        public string SwatchCard { get; set; }
        public string SwatchBorder { get; set; }
        public string SwatchAccent { get; set; }
    }

    public class PaletteOption
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public string AccentColor { get; set; }
        public string AccentHover { get; set; }
        public string DarkFocusColor { get; set; }
        public string LightFocusColor { get; set; }
        public string DarkDistractColor { get; set; }
        public string LightDistractColor { get; set; }
    }

    public static class FocusPulseStyles
    {
        public static Color HexToColor(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return Colors.White;
            hex = hex.Replace("#", "").Trim();
            byte a = 255;
            byte r = 0, g = 0, b = 0;
            if (hex.Length == 8)
            {
                a = Convert.ToByte(hex.Substring(0, 2), 16);
                r = Convert.ToByte(hex.Substring(2, 2), 16);
                g = Convert.ToByte(hex.Substring(4, 2), 16);
                b = Convert.ToByte(hex.Substring(6, 2), 16);
            }
            else if (hex.Length == 6)
            {
                r = Convert.ToByte(hex.Substring(0, 2), 16);
                g = Convert.ToByte(hex.Substring(2, 2), 16);
                b = Convert.ToByte(hex.Substring(4, 2), 16);
            }
            return Color.FromArgb(a, r, g, b);
        }

        public static SolidColorBrush Brush(string hex)
        {
            SolidColorBrush b = new SolidColorBrush(HexToColor(hex));
            b.Freeze();
            return b;
        }

        public static Style CreateSlimScrollBarStyle(SurfaceTheme theme)
        {
            string thumbColor = theme.IsDark ? "#48FFFFFF" : "#38000000";
            string thumbHover = theme.IsDark ? "#80FFFFFF" : "#68000000";
            string xaml = string.Format(@"
<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
       xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
       TargetType='ScrollBar'>
    <Setter Property='Stylus.IsPressAndHoldEnabled' Value='false'/>
    <Setter Property='Stylus.IsFlicksEnabled' Value='false'/>
    <Setter Property='Background' Value='Transparent'/>
    <Setter Property='Template'>
        <Setter.Value>
            <ControlTemplate TargetType='ScrollBar'>
                <Grid Background='Transparent'>
                    <Track x:Name='PART_Track' IsDirectionReversed='true'>
                        <Track.Thumb>
                            <Thumb>
                                <Thumb.Template>
                                    <ControlTemplate TargetType='Thumb'>
                                        <Border x:Name='ThumbBorder' Background='{0}' CornerRadius='2.5' Margin='1'/>
                                        <ControlTemplate.Triggers>
                                            <Trigger Property='IsMouseOver' Value='true'>
                                                <Setter TargetName='ThumbBorder' Property='Background' Value='{1}'/>
                                            </Trigger>
                                        </ControlTemplate.Triggers>
                                    </ControlTemplate>
                                </Thumb.Template>
                            </Thumb>
                        </Track.Thumb>
                    </Track>
                </Grid>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
    <Style.Triggers>
        <Trigger Property='Orientation' Value='Vertical'>
            <Setter Property='Width' Value='5'/>
            <Setter Property='MinWidth' Value='5'/>
        </Trigger>
        <Trigger Property='Orientation' Value='Horizontal'>
            <Setter Property='Height' Value='5'/>
            <Setter Property='MinHeight' Value='5'/>
        </Trigger>
    </Style.Triggers>
</Style>", thumbColor, thumbHover);

            try
            {
                return (Style)System.Windows.Markup.XamlReader.Parse(xaml);
            }
            catch
            {
                return null;
            }
        }

        public static readonly PaletteOption[] AvailablePalettes = new PaletteOption[]
        {
            new PaletteOption()
            {
                Key = "Sapphire",
                Name = "Electric Sapphire",
                AccentColor = "#0A84FF",
                AccentHover = "#38BDF8",
                DarkFocusColor = "#30D158",
                LightFocusColor = "#047857",
                DarkDistractColor = "#FF453A",
                LightDistractColor = "#BE123C"
            },
            new PaletteOption()
            {
                Key = "Violet",
                Name = "Tokyo Violet",
                AccentColor = "#A855F7",
                AccentHover = "#C084FC",
                DarkFocusColor = "#34D399",
                LightFocusColor = "#059669",
                DarkDistractColor = "#FB7185",
                LightDistractColor = "#BE123C"
            },
            new PaletteOption()
            {
                Key = "Emerald",
                Name = "Pixel Emerald",
                AccentColor = "#10B981",
                AccentHover = "#34D399",
                DarkFocusColor = "#34D399",
                LightFocusColor = "#047857",
                DarkDistractColor = "#FB7185",
                LightDistractColor = "#BE123C"
            },
            new PaletteOption()
            {
                Key = "Tangerine",
                Name = "Sunset Tangerine",
                AccentColor = "#F97316",
                AccentHover = "#FB923C",
                DarkFocusColor = "#10B981",
                LightFocusColor = "#047857",
                DarkDistractColor = "#F43F5E",
                LightDistractColor = "#BE123C"
            },
            new PaletteOption()
            {
                Key = "Rose",
                Name = "Rose Quartz",
                AccentColor = "#F43F5E",
                AccentHover = "#FB7185",
                DarkFocusColor = "#10B981",
                LightFocusColor = "#047857",
                DarkDistractColor = "#E11D48",
                LightDistractColor = "#BE123C"
            },
            new PaletteOption()
            {
                Key = "Minimal",
                Name = "Studio Slate",
                AccentColor = "#64748B",
                AccentHover = "#94A3B8",
                DarkFocusColor = "#34D399",
                LightFocusColor = "#047857",
                DarkDistractColor = "#FB7185",
                LightDistractColor = "#BE123C"
            }
        };

        public static SurfaceTheme ResolveTheme(bool isDark, string paletteKey)
        {
            if (string.IsNullOrEmpty(paletteKey)) paletteKey = "Sapphire";

            PaletteOption pal = null;
            foreach (PaletteOption p in AvailablePalettes)
            {
                if (p.Key.Equals(paletteKey, StringComparison.OrdinalIgnoreCase))
                {
                    pal = p;
                    break;
                }
            }
            if (pal == null) pal = AvailablePalettes[0];

            SurfaceTheme theme = new SurfaceTheme();
            theme.IsDark = isDark;
            theme.PaletteKey = pal.Key;
            theme.PaletteName = pal.Name;
            theme.Key = (isDark ? "Dark_" : "Light_") + pal.Key;
            theme.DisplayTitle = pal.Name + (isDark ? " (Dark)" : " (Light)");
            theme.AccentHex = pal.AccentColor;
            theme.BrandAccent = Brush(pal.AccentColor);
            theme.BrandAccentHover = Brush(pal.AccentHover);

            if (isDark)
            {
                theme.CanvasBg = Brush("#0A0E17");
                theme.SurfaceCard = Brush("#121927");
                theme.SurfaceCardHover = Brush("#1A2336");
                theme.SurfaceElevated = Brush("#222E45");
                theme.SurfaceElevatedHover = Brush("#2D3D5B");
                theme.SurfaceActiveTab = Brush("#273550");
                theme.HairlineBorder = Brush("#25334C");
                theme.SubtleBorder = Brush("#161F2E");
                theme.TextPrimary = Brush("#F8FAFC");
                theme.TextSecondary = Brush("#94A3B8");
                theme.TextMuted = Brush("#64748B");

                theme.FocusBadgeText = Brush(pal.DarkFocusColor);
                theme.FocusBadgeBg = Brush("#0C2618");
                theme.FocusBadgeBorder = Brush("#15803D");

                theme.GeneralBadgeText = Brush(pal.AccentColor);
                theme.GeneralBadgeBg = Brush("#162035");
                theme.GeneralBadgeBorder = Brush(pal.AccentColor);

                theme.DistractBadgeText = Brush(pal.DarkDistractColor);
                theme.DistractBadgeBg = Brush("#331215");
                theme.DistractBadgeBorder = Brush("#991B1B");

                theme.AmberBadgeText = Brush("#FBBF24");
                theme.AmberBadgeBg = Brush("#33200B");

                theme.VioletBadgeText = Brush("#C084FC");
                theme.VioletBadgeBg = Brush("#2B1545");

                theme.SwatchCanvas = "#0A0E17";
                theme.SwatchCard = "#121927";
                theme.SwatchBorder = "#25334C";
                theme.SwatchAccent = pal.AccentColor;
            }
            else
            {
                theme.CanvasBg = Brush("#F8FAFC");
                theme.SurfaceCard = Brush("#FFFFFF");
                theme.SurfaceCardHover = Brush("#F1F5F9");
                theme.SurfaceElevated = Brush("#E2E8F0");
                theme.SurfaceElevatedHover = Brush("#CBD5E1");
                theme.SurfaceActiveTab = Brush("#FFFFFF");
                theme.HairlineBorder = Brush("#E2E8F0");
                theme.SubtleBorder = Brush("#F1F5F9");
                theme.TextPrimary = Brush("#0F172A");
                theme.TextSecondary = Brush("#475569");
                theme.TextMuted = Brush("#94A3B8");

                theme.FocusBadgeText = Brush(pal.LightFocusColor);
                theme.FocusBadgeBg = Brush("#ECFDF5");
                theme.FocusBadgeBorder = Brush("#A7F3D0");

                theme.GeneralBadgeText = Brush(pal.AccentColor);
                theme.GeneralBadgeBg = Brush("#F0F9FF");
                theme.GeneralBadgeBorder = Brush("#BAE6FD");

                theme.DistractBadgeText = Brush(pal.LightDistractColor);
                theme.DistractBadgeBg = Brush("#FFF1F2");
                theme.DistractBadgeBorder = Brush("#FECDD3");

                theme.AmberBadgeText = Brush("#B45309");
                theme.AmberBadgeBg = Brush("#FFFBEB");

                theme.VioletBadgeText = Brush("#6D28D9");
                theme.VioletBadgeBg = Brush("#F5F3FF");

                theme.SwatchCanvas = "#F8FAFC";
                theme.SwatchCard = "#FFFFFF";
                theme.SwatchBorder = "#E2E8F0";
                theme.SwatchAccent = pal.AccentColor;
            }

            return theme;
        }

        public static SurfaceTheme ResolveTheme(string key)
        {
            if (string.IsNullOrEmpty(key)) return ResolveTheme(false, "Sapphire");

            bool isDark = false;
            string pal = "Sapphire";

            if (key.IndexOf("Dark", StringComparison.OrdinalIgnoreCase) >= 0 ||
                key.IndexOf("Oled", StringComparison.OrdinalIgnoreCase) >= 0 ||
                key.IndexOf("Obsidian", StringComparison.OrdinalIgnoreCase) >= 0 ||
                key.IndexOf("Midnight", StringComparison.OrdinalIgnoreCase) >= 0 ||
                key.IndexOf("Slate", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                isDark = true;
            }

            if (key.IndexOf("Violet", StringComparison.OrdinalIgnoreCase) >= 0 || key.IndexOf("Purple", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                pal = "Violet";
                if (key.Equals("CyberViolet", StringComparison.OrdinalIgnoreCase)) isDark = true;
            }
            else if (key.IndexOf("Emerald", StringComparison.OrdinalIgnoreCase) >= 0 || key.IndexOf("Green", StringComparison.OrdinalIgnoreCase) >= 0 || key.IndexOf("Material", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                pal = "Emerald";
            }
            else if (key.IndexOf("Tangerine", StringComparison.OrdinalIgnoreCase) >= 0 || key.IndexOf("Amber", StringComparison.OrdinalIgnoreCase) >= 0 || key.IndexOf("Orange", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                pal = "Tangerine";
            }
            else if (key.IndexOf("Rose", StringComparison.OrdinalIgnoreCase) >= 0 || key.IndexOf("Crimson", StringComparison.OrdinalIgnoreCase) >= 0 || key.IndexOf("Red", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                pal = "Rose";
            }
            else if (key.IndexOf("Minimal", StringComparison.OrdinalIgnoreCase) >= 0 || key.IndexOf("Mono", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                pal = "Minimal";
            }
            else
            {
                pal = "Sapphire";
            }

            if (key.StartsWith("Light_", StringComparison.OrdinalIgnoreCase)) isDark = false;
            if (key.StartsWith("Dark_", StringComparison.OrdinalIgnoreCase)) isDark = true;

            return ResolveTheme(isDark, pal);
        }

        public static readonly string[] SwatchPalette = new string[]
        {
            "#38BDF8", // Sky
            "#10B981", // Emerald
            "#F97316", // Orange
            "#A855F7", // Purple
            "#14B8A6", // Teal
            "#F59E0B", // Amber
            "#F43F5E", // Rose
            "#6366F1"  // Indigo
        };

        public static string GetSwatch(int index)
        {
            return SwatchPalette[Math.Abs(index) % SwatchPalette.Length];
        }

        public static SolidColorBrush ScoreToBrush(int score, SurfaceTheme theme)
        {
            if (score >= 80) return theme.FocusBadgeText;
            if (score >= 60) return theme.BrandAccent;
            if (score >= 40) return theme.AmberBadgeText;
            return theme.DistractBadgeText;
        }

        public static string GetAppGlyph(string friendlyName)
        {
            if (string.IsNullOrEmpty(friendlyName)) return "◆";
            string clean = friendlyName.Trim();
            if (clean.IndexOf("Antigravity", StringComparison.OrdinalIgnoreCase) >= 0) return "✦";
            if (clean.IndexOf("YouTube", StringComparison.OrdinalIgnoreCase) >= 0) return "▶";
            if (clean.IndexOf("Reddit", StringComparison.OrdinalIgnoreCase) >= 0) return "💬";
            if (clean.IndexOf("GitHub", StringComparison.OrdinalIgnoreCase) >= 0 || clean.IndexOf("GitLab", StringComparison.OrdinalIgnoreCase) >= 0) return "⚡";
            if (clean.IndexOf("Stack Overflow", StringComparison.OrdinalIgnoreCase) >= 0) return "💡";
            if (clean.IndexOf("ChatGPT", StringComparison.OrdinalIgnoreCase) >= 0 || clean.IndexOf("Claude", StringComparison.OrdinalIgnoreCase) >= 0 || clean.IndexOf("Gemini", StringComparison.OrdinalIgnoreCase) >= 0) return "✦";
            if (clean.IndexOf("Wikipedia", StringComparison.OrdinalIgnoreCase) >= 0 || clean.IndexOf("Study", StringComparison.OrdinalIgnoreCase) >= 0 || clean.IndexOf("Research", StringComparison.OrdinalIgnoreCase) >= 0 || clean.IndexOf("Course", StringComparison.OrdinalIgnoreCase) >= 0) return "📚";
            if (clean.IndexOf("Google Workspace", StringComparison.OrdinalIgnoreCase) >= 0 || clean.IndexOf("Docs", StringComparison.OrdinalIgnoreCase) >= 0) return "📄";
            if (clean.IndexOf("Visual Studio Code", StringComparison.OrdinalIgnoreCase) >= 0 || clean.IndexOf("Code", StringComparison.OrdinalIgnoreCase) >= 0) return "⌥";
            if (clean.IndexOf("Cursor", StringComparison.OrdinalIgnoreCase) >= 0) return "✦";
            if (clean.IndexOf("Windsurf", StringComparison.OrdinalIgnoreCase) >= 0) return "🏄";
            if (clean.IndexOf("Chrome", StringComparison.OrdinalIgnoreCase) >= 0) return "🌐";
            if (clean.IndexOf("Edge", StringComparison.OrdinalIgnoreCase) >= 0) return "🌊";
            if (clean.IndexOf("Brave", StringComparison.OrdinalIgnoreCase) >= 0) return "🦁";
            if (clean.IndexOf("Firefox", StringComparison.OrdinalIgnoreCase) >= 0) return "🦊";
            if (clean.IndexOf("Terminal", StringComparison.OrdinalIgnoreCase) >= 0 || clean.IndexOf("PowerShell", StringComparison.OrdinalIgnoreCase) >= 0) return "❯";
            if (clean.IndexOf("Notion", StringComparison.OrdinalIgnoreCase) >= 0) return "📝";
            if (clean.IndexOf("Obsidian", StringComparison.OrdinalIgnoreCase) >= 0) return "💎";
            if (clean.IndexOf("Figma", StringComparison.OrdinalIgnoreCase) >= 0) return "🎨";
            if (clean.IndexOf("Discord", StringComparison.OrdinalIgnoreCase) >= 0) return "💬";
            if (clean.IndexOf("Slack", StringComparison.OrdinalIgnoreCase) >= 0) return "🗯";
            if (clean.IndexOf("Teams", StringComparison.OrdinalIgnoreCase) >= 0) return "👥";
            if (clean.IndexOf("Spotify", StringComparison.OrdinalIgnoreCase) >= 0) return "🎵";
            if (clean.IndexOf("Steam", StringComparison.OrdinalIgnoreCase) >= 0) return "🎮";
            if (clean.IndexOf("Explorer", StringComparison.OrdinalIgnoreCase) >= 0) return "📁";
            if (clean.IndexOf("LinkedIn", StringComparison.OrdinalIgnoreCase) >= 0) return "💼";
            if (clean.IndexOf("ONLYOFFICE", StringComparison.OrdinalIgnoreCase) >= 0 || clean.IndexOf("Office", StringComparison.OrdinalIgnoreCase) >= 0) return "📄";
            if (clean.IndexOf("Snipping", StringComparison.OrdinalIgnoreCase) >= 0) return "✂";
            if (clean.IndexOf("Store", StringComparison.OrdinalIgnoreCase) >= 0) return "🛍";
            if (clean.IndexOf("WhatsApp", StringComparison.OrdinalIgnoreCase) >= 0) return "💬";
            if (clean.IndexOf("Postman", StringComparison.OrdinalIgnoreCase) >= 0) return "🚀";
            if (clean.IndexOf("OpenCode", StringComparison.OrdinalIgnoreCase) >= 0) return "⌥";
            if (clean.IndexOf("Twitter", StringComparison.OrdinalIgnoreCase) >= 0 || clean == "X" || clean.StartsWith("X ")) return "𝕏";
            if (clean.Length > 0)
            {
                return clean.Substring(0, 1).ToUpper();
            }
            return "◆";
        }

        public static System.Drawing.Icon CreateFallbackIcon()
        {
            try
            {
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FocusPulse.ico");
                if (!File.Exists(iconPath))
                {
                    iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "src", "FocusPulse.ico");
                }
                if (File.Exists(iconPath))
                {
                    return new System.Drawing.Icon(iconPath);
                }
            }
            catch { }

            try
            {
                string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                {
                    System.Drawing.Icon appIcon = System.Drawing.Icon.ExtractAssociatedIcon(exePath);
                    if (appIcon != null) return appIcon;
                }
            }
            catch { }

            // Dynamic in-memory GDI icon fallback
            try
            {
                using (System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(32, 32))
                {
                    using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bmp))
                    {
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        g.Clear(System.Drawing.Color.Transparent);

                        using (System.Drawing.SolidBrush bg = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(255, 14, 18, 28)))
                        {
                            g.FillEllipse(bg, 1, 1, 30, 30);
                        }
                        using (System.Drawing.Pen borderPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(255, 56, 189, 248), 2.0f))
                        {
                            g.DrawEllipse(borderPen, 1, 1, 30, 30);
                        }

                        System.Drawing.PointF[] pts = new System.Drawing.PointF[8];
                        float cx = 16, cy = 16, r = 9, ir = 2.5f;
                        pts[0] = new System.Drawing.PointF(cx, cy - r);
                        pts[1] = new System.Drawing.PointF(cx + ir, cy - ir);
                        pts[2] = new System.Drawing.PointF(cx + r, cy);
                        pts[3] = new System.Drawing.PointF(cx + ir, cy + ir);
                        pts[4] = new System.Drawing.PointF(cx, cy + r);
                        pts[5] = new System.Drawing.PointF(cx - ir, cy + ir);
                        pts[6] = new System.Drawing.PointF(cx - r, cy);
                        pts[7] = new System.Drawing.PointF(cx - ir, cy - ir);

                        using (System.Drawing.SolidBrush starBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(255, 16, 185, 129)))
                        {
                            g.FillPolygon(starBrush, pts);
                        }
                    }
                    IntPtr hIcon = bmp.GetHicon();
                    return System.Drawing.Icon.FromHandle(hIcon);
                }
            }
            catch
            {
                return System.Drawing.SystemIcons.Application;
            }
        }
    }
}
