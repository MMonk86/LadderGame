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
        private NumericUpDown nudComplexity;
        private Button btnStart;
        private Button btnRandom;
        private Button btnPlay;
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

        // Animation logic
        private Timer animTimer;
        private bool isAnimating = false;
        private float animSpeed = 5.0f; // Pixels per tick
        private List<Runner> runners = new List<Runner>();

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

            Label lblComplexity = new Label();
            lblComplexity.Text = "복잡도:";
            lblComplexity.Location = new Point(140, 15);
            lblComplexity.AutoSize = true;
            this.Controls.Add(lblComplexity);

            nudComplexity = new NumericUpDown();
            nudComplexity.Location = new Point(190, 12);
            nudComplexity.Width = 40;
            nudComplexity.Minimum = 1;
            nudComplexity.Maximum = 20;
            nudComplexity.Value = 5;
            nudComplexity.ValueChanged += NudComplexity_ValueChanged;
            this.Controls.Add(nudComplexity);

            btnRandom = new Button();
            btnRandom.Text = "랜덤 섞기";
            btnRandom.Location = new Point(240, 10);
            btnRandom.Click += BtnRandom_Click;
            this.Controls.Add(btnRandom);

            btnStart = new Button();
            btnStart.Text = "시작";
            btnStart.Location = new Point(330, 10);
            btnStart.Click += BtnStart_Click;
            this.Controls.Add(btnStart);

            btnPlay = new Button();
            btnPlay.Text = "Play";
            btnPlay.Location = new Point(420, 10);
            btnPlay.Click += BtnPlay_Click;
            this.Controls.Add(btnPlay);

            animTimer = new Timer();
            animTimer.Interval = 20; // 50fps
            animTimer.Tick += AnimTimer_Tick;

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

        private void NudComplexity_ValueChanged(object sender, EventArgs e)
        {
            GenerateLadder();
            highlightedPathIndex = null;
            pathPoints.Clear();
            isAnimating = false;
            pnlLadder.Invalidate();
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
            isAnimating = false;
            animTimer.Stop();

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
            // Generate random bridges (Horizontal and Diagonal)
            // A diagonal bridge connects Col[LeftY] to Col+1[RightY]

            // Determine density from UI
            int densityBase = (int)nudComplexity.Value;

            for (int col = 0; col < participants - 1; col++)
            {
                // Number of bridges in this column gap based on complexity
                int bridgesInCol = densityBase + rng.Next(-1, 2); // +/- 1 variance
                if (bridgesInCol < 1) bridgesInCol = 1;

                for (int k = 0; k < bridgesInCol; k++)
                {
                    float leftY = (float)(0.1 + (rng.NextDouble() * 0.8));

                    // 30% chance of being diagonal
                    float rightY = leftY;
                    if (rng.NextDouble() < 0.3)
                    {
                        // Slight slant: +/- 0.05 height
                        float offset = (float)((rng.NextDouble() - 0.5) * 0.1);
                        rightY = leftY + offset;
                    }

                    // Basic clamp
                    if (rightY < 0.1f) rightY = 0.1f;
                    if (rightY > 0.9f) rightY = 0.9f;

                    bridges.Add(new Bridge { ColIndex = col, LeftYPercent = leftY, RightYPercent = rightY });
                }
            }
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

            GenerateLadder();
            highlightedPathIndex = null;
            pathPoints.Clear();
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

        private void BtnPlay_Click(object sender, EventArgs e)
        {
            if (isAnimating || participants < 2) return;

            runners.Clear();
            Color[] colors = new Color[] { Color.Red, Color.Blue, Color.Green, Color.Orange, Color.Purple, Color.Magenta, Color.Brown, Color.Teal, Color.Olive, Color.Navy };

            for (int i = 0; i < participants; i++)
            {
                var path = GetPathForParticipant(i);
                if (path.Count > 0)
                {
                    runners.Add(new Runner
                    {
                        Path = path,
                        CurrentIndex = 0,
                        CurrentPos = path[0],
                        RunnerColor = colors[i % colors.Length],
                        Finished = false
                    });
                }
            }

            isAnimating = true;
            animTimer.Start();
        }

        private void AnimTimer_Tick(object sender, EventArgs e)
        {
            if (!isAnimating || runners.Count == 0)
            {
                animTimer.Stop();
                isAnimating = false;
                pnlLadder.Invalidate();
                return;
            }

            bool allFinished = true;

            foreach (var runner in runners)
            {
                if (runner.Finished) continue;
                allFinished = false;

                if (runner.CurrentIndex >= runner.Path.Count - 1)
                {
                    runner.Finished = true;
                    continue;
                }

                PointF target = runner.Path[runner.CurrentIndex + 1];
                float dx = target.X - runner.CurrentPos.X;
                float dy = target.Y - runner.CurrentPos.Y;
                float dist = (float)Math.Sqrt(dx * dx + dy * dy);

                if (dist < animSpeed)
                {
                    runner.CurrentPos = target;
                    runner.CurrentIndex++;
                }
                else
                {
                    float moveX = (dx / dist) * animSpeed;
                    float moveY = (dy / dist) * animSpeed;
                    runner.CurrentPos = new PointF(runner.CurrentPos.X + moveX, runner.CurrentPos.Y + moveY);
                }
            }

            if (allFinished)
            {
                animTimer.Stop();
                isAnimating = false;
                string results = "";
                for (int i = 0; i < participants; i++)
                {
                    string result = GetResultFor(i);
                    results += $"{topInputs[i].Text} -> {result}\n";
                }
                MessageBox.Show(results, "결과 확인");
            }

            pnlLadder.Invalidate();
        }

        private List<PointF> GetPathForParticipant(int startCol)
        {
            var points = new List<PointF>();
            int currentCol = startCol;
            float currentY = 0f;

            int width = pnlLadder.Width;
            int height = pnlLadder.Height;
            int colWidth = width / participants;

            points.Add(new PointF(GetColX(currentCol, colWidth), 0));

            while (currentY < 1.0f)
            {
                var nextLeftBridge = bridges
                    .Where(b => b.ColIndex == currentCol && b.LeftYPercent > currentY + 0.001f)
                    .OrderBy(b => b.LeftYPercent)
                    .FirstOrDefault();

                var nextRightBridge = bridges
                    .Where(b => b.ColIndex == currentCol - 1 && b.RightYPercent > currentY + 0.001f)
                    .OrderBy(b => b.RightYPercent)
                    .FirstOrDefault();

                float nextY = 1.0f;
                Bridge targetBridge = null;
                bool goingRight = false;

                if (nextLeftBridge != null && nextRightBridge != null)
                {
                    if (nextLeftBridge.LeftYPercent < nextRightBridge.RightYPercent)
                    {
                        nextY = nextLeftBridge.LeftYPercent;
                        targetBridge = nextLeftBridge;
                        goingRight = true;
                    }
                    else
                    {
                        nextY = nextRightBridge.RightYPercent;
                        targetBridge = nextRightBridge;
                        goingRight = false;
                    }
                }
                else if (nextLeftBridge != null)
                {
                    nextY = nextLeftBridge.LeftYPercent;
                    targetBridge = nextLeftBridge;
                    goingRight = true;
                }
                else if (nextRightBridge != null)
                {
                    nextY = nextRightBridge.RightYPercent;
                    targetBridge = nextRightBridge;
                    goingRight = false;
                }

                // Add segment going down to bridge or bottom
                points.Add(new PointF(GetColX(currentCol, colWidth), nextY * height));
                currentY = nextY;

                if (currentY >= 1.0f) break;

                // Traverse the bridge
                if (targetBridge != null)
                {
                    if (goingRight)
                    {
                        // From Left to Right
                        currentCol++;
                        currentY = targetBridge.RightYPercent;
                        points.Add(new PointF(GetColX(currentCol, colWidth), currentY * height));
                    }
                    else
                    {
                        // From Right to Left
                        currentCol--;
                        currentY = targetBridge.LeftYPercent;
                        points.Add(new PointF(GetColX(currentCol, colWidth), currentY * height));
                    }
                }
            }
            return points;
        }

        private void CalculatePath(int startCol, bool showMessage = true)
        {
            highlightedPathIndex = startCol;
            pathPoints = GetPathForParticipant(startCol);
            pnlLadder.Invalidate();

            if (showMessage)
            {
                string startName = topInputs[startCol].Text;
                // Get result by following path to end
                // We can just use GetResultFor(startCol) for simplicity as it duplicates logic but is robust
                string result = GetResultFor(startCol);
                MessageBox.Show($"{startName} -> {result}");
            }
        }

        private string GetResultFor(int startCol)
        {
            int currentCol = startCol;
            float currentY = 0f;

            while (currentY < 1.0f)
            {
                var nextLeftBridge = bridges
                    .Where(b => b.ColIndex == currentCol && b.LeftYPercent > currentY + 0.001f)
                    .OrderBy(b => b.LeftYPercent)
                    .FirstOrDefault();

                var nextRightBridge = bridges
                    .Where(b => b.ColIndex == currentCol - 1 && b.RightYPercent > currentY + 0.001f)
                    .OrderBy(b => b.RightYPercent)
                    .FirstOrDefault();

                Bridge targetBridge = null;
                bool goingRight = false;
                float nextY = 1.0f;

                if (nextLeftBridge != null && nextRightBridge != null)
                {
                    if (nextLeftBridge.LeftYPercent < nextRightBridge.RightYPercent)
                    {
                        nextY = nextLeftBridge.LeftYPercent;
                        targetBridge = nextLeftBridge;
                        goingRight = true;
                    }
                    else
                    {
                        nextY = nextRightBridge.RightYPercent;
                        targetBridge = nextRightBridge;
                        goingRight = false;
                    }
                }
                else if (nextLeftBridge != null)
                {
                    nextY = nextLeftBridge.LeftYPercent;
                    targetBridge = nextLeftBridge;
                    goingRight = true;
                }
                else if (nextRightBridge != null)
                {
                    nextY = nextRightBridge.RightYPercent;
                    targetBridge = nextRightBridge;
                    goingRight = false;
                }

                currentY = nextY;
                if (currentY >= 1.0f) break;

                if (targetBridge != null)
                {
                    if (goingRight)
                    {
                        currentCol++;
                        currentY = targetBridge.RightYPercent;
                    }
                    else
                    {
                        currentCol--;
                        currentY = targetBridge.LeftYPercent;
                    }
                }
            }
            return bottomInputs[currentCol].Text;
        }

        private void PnlLadder_MouseClick(object sender, MouseEventArgs e)
        {
            if (isAnimating) return;

             // Determine which column was clicked
            int colWidth = pnlLadder.Width / participants;
            int clickedCol = e.X / colWidth;

            if (clickedCol >= 0 && clickedCol < participants)
            {
                CalculatePath(clickedCol, true);
            }
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
                float y1 = bridge.LeftYPercent * height;
                float y2 = bridge.RightYPercent * height;
                g.DrawLine(linePen, x1, y1, x2, y2);
            }

            // Draw Highlighted Path
            if (!isAnimating && pathPoints.Count > 1)
            {
                g.DrawLines(pathPen, pathPoints.ToArray());
            }

            // Draw Animation Objects
            if (isAnimating)
            {
                float r = 8;
                foreach (var runner in runners)
                {
                    using (Brush b = new SolidBrush(runner.RunnerColor))
                    {
                        g.FillEllipse(b, runner.CurrentPos.X - r, runner.CurrentPos.Y - r, r * 2, r * 2);
                    }
                }
            }
        }
    }

    public class Bridge
    {
        public int ColIndex; // The bridge connects ColIndex and ColIndex + 1
        public float LeftYPercent;
        public float RightYPercent;
    }

    public class Runner
    {
        public List<PointF> Path;
        public int CurrentIndex;
        public PointF CurrentPos;
        public Color RunnerColor;
        public bool Finished;
    }
}
