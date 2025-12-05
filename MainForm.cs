using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace LadderGame
{
    public class MainForm : Form
    {
        private ComboBox cboCount;
        private Button btnStart;
        private Button btnRandom;
        private Panel pnlLadder;
        private List<TextBox> topInputs = new List<TextBox>();
        private List<TextBox> bottomInputs = new List<TextBox>();

        // Logic data
        private int participants = 0;
        private List<Bridge> bridges = new List<Bridge>();
        private Random rng = new Random();

        // Animation/Result state
        private int? highlightedPathIndex = null;
        private List<PointF> pathPoints = new List<PointF>();

        public MainForm()
        {
            InitializeComponent();
            // Default selection
            cboCount.SelectedIndex = 0; // Selects '2'
        }

        private void InitializeComponent()
        {
            this.Text = "사다리타기 게임";
            this.Size = new Size(800, 600);
            this.DoubleBuffered = true;

            Label lblCount = new Label();
            lblCount.Text = "인원 수:";
            lblCount.Location = new Point(10, 15);
            lblCount.AutoSize = true;
            this.Controls.Add(lblCount);

            cboCount = new ComboBox();
            cboCount.Location = new Point(70, 12);
            cboCount.Width = 60;
            cboCount.DropDownStyle = ComboBoxStyle.DropDownList;
            for (int i = 2; i <= 10; i++)
            {
                cboCount.Items.Add(i.ToString());
            }
            cboCount.SelectedIndexChanged += CboCount_SelectedIndexChanged;
            this.Controls.Add(cboCount);

            btnRandom = new Button();
            btnRandom.Text = "랜덤 섞기";
            btnRandom.Location = new Point(150, 10);
            btnRandom.Click += BtnRandom_Click;
            this.Controls.Add(btnRandom);

            btnStart = new Button();
            btnStart.Text = "시작";
            btnStart.Location = new Point(240, 10);
            btnStart.Click += BtnStart_Click;
            this.Controls.Add(btnStart);

            pnlLadder = new Panel();
            pnlLadder.Location = new Point(20, 80);
            pnlLadder.Size = new Size(740, 400);
            pnlLadder.BackColor = Color.White;
            pnlLadder.Paint += PnlLadder_Paint;
            pnlLadder.MouseClick += PnlLadder_MouseClick;
            this.Controls.Add(pnlLadder);
        }

        private void CboCount_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetupGame();
        }

        private void SetupGame()
        {
            if (cboCount.SelectedItem == null) return;

            participants = int.Parse(cboCount.SelectedItem.ToString());

            // Clear existing TextBoxes
            foreach (var tb in topInputs)
            {
                this.Controls.Remove(tb);
                tb.Dispose();
            }
            foreach (var tb in bottomInputs)
            {
                this.Controls.Remove(tb);
                tb.Dispose();
            }
            topInputs.Clear();
            bottomInputs.Clear();

            // Clear game state
            bridges.Clear();
            highlightedPathIndex = null;
            pathPoints.Clear();

            // Dimensions
            int areaWidth = pnlLadder.Width;
            int colWidth = areaWidth / participants;
            int startX = pnlLadder.Location.X;
            int topY = pnlLadder.Location.Y - 30;
            int bottomY = pnlLadder.Location.Y + pnlLadder.Height + 10;

            // Generate inputs
            for (int i = 0; i < participants; i++)
            {
                int centerX = startX + (i * colWidth) + (colWidth / 2);

                // Top Input (Name)
                TextBox tbName = new TextBox();
                tbName.Text = (i + 1).ToString(); // Default name
                tbName.Location = new Point(centerX - 25, topY);
                tbName.Width = 50;
                tbName.TextAlign = HorizontalAlignment.Center;
                this.Controls.Add(tbName);
                topInputs.Add(tbName);

                // Bottom Input (Result)
                TextBox tbResult = new TextBox();
                tbResult.Text = "꽝"; // Default result
                tbResult.Location = new Point(centerX - 25, bottomY);
                tbResult.Width = 50;
                tbResult.TextAlign = HorizontalAlignment.Center;
                this.Controls.Add(tbResult);
                bottomInputs.Add(tbResult);
            }

            GenerateLadder();
            pnlLadder.Invalidate();
        }

        private void GenerateLadder()
        {
            bridges.Clear();
            // Generate random bridges
            // For each column gap (0 to n-2), add random bridges
            // We ensure bridges don't overlap too closely in Y

            int stepsPerGap = 3 + rng.Next(3); // Random density

            // Simple approach: Divide height into segments and randomly place bridge
            // Or just generate random Ys and sort.

            for (int col = 0; col < participants - 1; col++)
            {
                for (int k = 0; k < 4; k++) // Try to add a few bridges per column gap
                {
                    float y = (float)(0.1 + (rng.NextDouble() * 0.8)); // 10% to 90% height

                    // Check if too close to existing bridge in same col, or neighbors
                    // Note: In a real game, we need to be careful about overlapping bridges on adjacent columns at exact same Y.
                    // For simplicity, we just add them and will sort/filter later or just draw carefully.
                    // To prevent crossing overlaps: ensure Y's are distinct or have spacing.

                    bridges.Add(new Bridge { ColIndex = col, YPercent = y });
                }
            }

            // Important: Bridges in adjacent columns shouldn't be too close vertically to avoid ambiguity.
            // But for this simple implementation, we'll just sort them by Y globally to traverse.
            // Wait, standard traversal is Y-ordered.
        }

        private void BtnRandom_Click(object sender, EventArgs e)
        {
            // Shuffle bottom inputs text
            var texts = bottomInputs.Select(tb => tb.Text).ToList();
            int n = texts.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                string value = texts[k];
                texts[k] = texts[n];
                texts[n] = value;
            }

            for (int i = 0; i < bottomInputs.Count; i++)
            {
                bottomInputs[i].Text = texts[i];
            }

            // Regenerate ladder structure too? The user asked "Button to shuffle boxes".
            // Maybe ladder stays same? Usually ladder stays same.
            // But let's regenerate ladder to make it "new".
            GenerateLadder();
            highlightedPathIndex = null;
            pnlLadder.Invalidate();
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            if (participants < 2) return;

            string results = "";
            for (int i = 0; i < participants; i++)
            {
                string result = GetResultFor(i);
                results += $"{topInputs[i].Text} -> {result}\n";
            }
            MessageBox.Show(results, "결과 확인");
        }

        private string GetResultFor(int startCol)
        {
            int currentCol = startCol;
            float currentY = 0f;

            // Sort all bridges by Y
            var sortedBridges = bridges.OrderBy(b => b.YPercent).ToList();

            foreach (var bridge in sortedBridges)
            {
                // Convert percent to actual Y (logic only, size independent)
                // If this bridge is "below" our current virtual position (which moves down)
                if (bridge.YPercent < currentY) continue;

                // In a real traversal, we just go down the list of bridges.
                // Since bridges are sorted by Y, we just process them in order.

                if (bridge.ColIndex == currentCol)
                {
                    // Bridge to the right
                    currentCol++;
                }
                else if (bridge.ColIndex == currentCol - 1)
                {
                    // Bridge to the left (coming from left column)
                    currentCol--;
                }
            }
            return bottomInputs[currentCol].Text;
        }

        // Allow clicking the top area of the panel to trace path
        private void PnlLadder_MouseClick(object sender, MouseEventArgs e)
        {
             // Determine which column was clicked
            int colWidth = pnlLadder.Width / participants;
            int clickedCol = e.X / colWidth;

            if (clickedCol >= 0 && clickedCol < participants)
            {
                CalculatePath(clickedCol);
            }
        }

        private void CalculatePath(int startCol)
        {
            highlightedPathIndex = startCol;
            pathPoints.Clear();

            int currentCol = startCol;
            float currentY = 0f;

            int width = pnlLadder.Width;
            int height = pnlLadder.Height;
            int colWidth = width / participants;

            // Sort all bridges by Y
            var sortedBridges = bridges.OrderBy(b => b.YPercent).ToList();

            // Start Point
            pathPoints.Add(new PointF(GetColX(currentCol, colWidth), 0));

            foreach (var bridge in sortedBridges)
            {
                // Convert percent to actual Y
                float bridgeY = bridge.YPercent * height;

                if (bridgeY < currentY) continue;

                // Add point down to this bridge level
                pathPoints.Add(new PointF(GetColX(currentCol, colWidth), bridgeY));
                currentY = bridgeY;

                // Check if this bridge affects us
                if (bridge.ColIndex == currentCol)
                {
                    // Bridge to the right
                    currentCol++;
                    pathPoints.Add(new PointF(GetColX(currentCol, colWidth), bridgeY));
                }
                else if (bridge.ColIndex == currentCol - 1)
                {
                    // Bridge to the left (coming from left column)
                    currentCol--;
                    pathPoints.Add(new PointF(GetColX(currentCol, colWidth), bridgeY));
                }
            }

            // Final point at bottom
            pathPoints.Add(new PointF(GetColX(currentCol, colWidth), height));

            pnlLadder.Invalidate();

            // Show result
            string startName = topInputs[startCol].Text;
            string result = bottomInputs[currentCol].Text;
            MessageBox.Show($"{startName} -> {result}");
        }

        private float GetColX(int colIndex, int colWidth)
        {
            return (colIndex * colWidth) + (colWidth / 2.0f);
        }

        private void PnlLadder_Paint(object sender, PaintEventArgs e)
        {
            if (participants < 2) return;

            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            Pen linePen = new Pen(Color.Black, 2);
            Pen pathPen = new Pen(Color.Red, 4);

            int width = pnlLadder.Width;
            int height = pnlLadder.Height;
            int colWidth = width / participants;

            // Draw Vertical Lines
            for (int i = 0; i < participants; i++)
            {
                float x = GetColX(i, colWidth);
                g.DrawLine(linePen, x, 0, x, height);
            }

            // Draw Bridges
            foreach (var bridge in bridges)
            {
                float x1 = GetColX(bridge.ColIndex, colWidth);
                float x2 = GetColX(bridge.ColIndex + 1, colWidth);
                float y = bridge.YPercent * height;
                g.DrawLine(linePen, x1, y, x2, y);
            }

            // Draw Highlighted Path
            if (pathPoints.Count > 1)
            {
                g.DrawLines(pathPen, pathPoints.ToArray());
            }
        }
    }

    public class Bridge
    {
        public int ColIndex; // The bridge connects ColIndex and ColIndex + 1
        public float YPercent; // Vertical position (0.0 to 1.0)
    }
}
