using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Glee;
using Microsoft.Glee.Splines;
using P = Microsoft.Glee.Splines.Point;
using System.Drawing.Drawing2D;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;

namespace DrawingFromGleeGraph {
    public partial class FormGr : Form
    {
        GleeGraph gleeGraph1, gleeGraph2;

        public string[] Nodes1;
        public string[] Nodes2;

        public string[] FO1;
        public string[] FO2;

        public int N = 20;
        public int E = 50;

        public FormGr()
        {
            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.SizeChanged += new EventHandler(FormGr_SizeChanged);
        }

        void FormGr_SizeChanged(object sender, EventArgs e)
        {
            this.Invalidate();
        }

        public bool CompareCycles(string[] n1, List<string> c1, string[] n2, List<string> c2)
        {
            if ((c1.Count() != c2.Count()) || (n1.Length != n2.Length))
                return false;

            int[] N1 = new int[c1.Count()];
            int[] N2 = new int[c2.Count()];

            int min1 = Int32.MaxValue;
            int min2 = Int32.MaxValue;

            for (int i = 0; i < c1.Count(); i++)
            {
                if (n1[0].IsAlpha())
                    N1[i] = Convert.ToInt32(c1.ElementAt(i).ToCharArray());
                else
                    N1[i] = Convert.ToInt32(c1.ElementAt(i));
                if (N1[i] < min1)
                    min1 = N1[i];

                if (n2[0].IsAlpha())
                    N2[i] = Convert.ToInt32(c2.ElementAt(i).ToCharArray()[0]);
                else
                    N2[i] = Convert.ToInt32(c2.ElementAt(i));
                if (N2[i] < min2)
                    min2 = N2[i];
            }
            if ((N1[0] != N1[c1.Count() - 1]) || (N2[0] != N2[c1.Count() - 1]))
                return false;

            return true;
        }
        
        public void EulerCycle(Edge v, List<Edge> edges, List<string> cycle)
        {         
            while(edges.Count() > 0)
            {
                cycle.Add(v.Source.Id);
                Node n = v.Target;
                edges.Remove(v);
                for (int i = n.OutEdges.Count() - 1; i >= 0; i--)
                {
                    Edge w = n.OutEdges.ElementAt(i);
                    if (edges.Exists(x => (x.Source.Id == w.Source.Id && x.Target.Id == w.Target.Id)))
                    { 
                        v = w;
                        break;
                    }
                }
            }
        }
         void DFSUtil(GleeGraph g, string[] nodes, int v, bool[] visited)
        {
            visited[v] = true;
            Node n = g.FindNode(nodes[v]);
            for (int i = 0; i < n.OutEdges.Count(); i++)
            {
                string target = n.OutEdges.ElementAt(i).Target.Id;           

                for (int j = 0; j < nodes.Length; j++)
                {
                    if (target == nodes[j])
                    {                            
                        if (visited[j] == false)
                        {                        
                            DFSUtil(g, nodes, j, visited);
                            break;                        
                        }                    
                    }
                }
            }
        }
        
        public Boolean isEulerianCycle(GleeGraph g, string[] nodes)
        {
            Boolean[] visited = new Boolean[g.NodeCount];
            for (int i = 0; i < g.NodeCount; i++)
                visited[i] = false;
            DFSUtil(g, nodes, 0, visited);
            for (int i = 0; i < g.NodeCount; i++)
               if (visited[i] == false)
                    return false;
               return true;
        }
        
        GleeGraph getTranspose(GleeGraph g, string[] nodes)
        {
            GleeGraph t = new GleeGraph();

            for (int v = 0; v < g.NodeCount; v++)
            {
                t.AddNode(g.FindNode(nodes[v]));
            }

            for (int v = 0; v < g.EdgeCount; v++)
            {
                Node source = g.Edges[v].Source;
                Node target = g.Edges[v].Target;
                Edge e = new Edge(target, source);
                t.AddEdge(e);
            }
            return t;
        }

        private void DrawFromGraph(Graphics graphics, GleeGraph gleeGraph, int n)
        {
            SetGraphicsTransform(graphics, gleeGraph, n);
            Pen pen = new Pen(Brushes.Black);
            DrawNodes(pen, graphics, gleeGraph);
            DrawEdges(pen, graphics, gleeGraph);
        }

        private void SetGraphicsTransform(Graphics graphics, GleeGraph gleeGraph, int n)
        {

            RectangleF r1 = this.ClientRectangle;
            RectangleF r = new RectangleF(r1.Left + 10 + n*(r1.Width - 440) / 2, r1.Top + 25, (r1.Width - 440) / 2, r1.Height - 50);

            Microsoft.Glee.Splines.Rectangle gr = gleeGraph.BoundingBox;
            if (r.Height > 1 && r.Width > 1)
            {
                float scale = Math.Min(r.Width / (float)gr.Width, r.Height / (float)gr.Height);
                float g0 = (float)(gr.Left + gr.Right) / 2;
                float g1 = (float)(gr.Top + gr.Bottom) / 2;

                float c0 = (r.Left + r.Right) / 2;
                float c1 = (r.Top + r.Bottom) / 2;
                float dx = c0 - scale * g0;
                float dy = c1 + scale * g1;
                graphics.Transform = new System.Drawing.Drawing2D.Matrix(scale, 0 , 0, -scale, dx, dy);   
            }
        }

        private void DrawEdges(Pen pen, Graphics graphics, GleeGraph gleeGraph)
        {
            foreach (Edge e in gleeGraph.Edges)
                DrawEdge(e, pen, graphics);
        }

        private void DrawEdge(Edge e, Pen pen, Graphics graphics)
        {
            ICurve curve = e.Curve;
            Curve c = curve as Curve;
            if (c != null)
            {
                foreach (ICurve s in c.Segs)
                {
                    LineSeg l = s as LineSeg;
                    if (l != null)
                        graphics.DrawLine(pen, GleePointToDrawingPoint(l.Start), GleePointToDrawingPoint(l.End));
                    CubicBezierSeg cs = s as CubicBezierSeg;
                    if (cs != null)
                        graphics.DrawBezier(pen, GleePointToDrawingPoint(cs.B(0)), GleePointToDrawingPoint(cs.B(1)), GleePointToDrawingPoint(cs.B(2)), GleePointToDrawingPoint(cs.B(3)));

                }
                if (e.ArrowHeadAtSource)
                    DrawArrow(e, pen, graphics, e.Curve.Start, e.ArrowHeadAtSourcePosition);
                if (e.ArrowHeadAtTarget)
                    DrawArrow(e, pen, graphics, e.Curve.End, e.ArrowHeadAtTargetPosition);
            }
        }

        private void DrawArrow(Edge e, Pen pen, Graphics graphics, P start, P end)
        {
            PointF[] points;
            float arrowAngle = 30;

            P dir = end - start;
            P h = dir;
            dir /= dir.Length;

            P s = new P(-dir.Y, dir.X);

            s *= h.Length * ((float)Math.Tan(arrowAngle * 0.5f * (Math.PI / 180.0)));

            points = new PointF[] { GleePointToDrawingPoint(start + s), GleePointToDrawingPoint(end), GleePointToDrawingPoint(start - s) };

            graphics.FillPolygon(pen.Brush, points);
        }

        private void DrawNodes(Pen pen, Graphics graphics, GleeGraph gleeGraph)
        {
            foreach (Node n in gleeGraph.NodeMap.Values)
                DrawNode(n, pen, graphics);
        }

        private void DrawNode(Node n, Pen pen, Graphics graphics)
        {
            ICurve curve = n.MovedBoundaryCurve;
            Ellipse el = curve as Ellipse;
            if (el != null)
            {
                RectangleF rec = new RectangleF((float)el.BBox.Left, (float)el.BBox.Bottom,
                    (float)el.BBox.Width, (float)el.BBox.Height);

                 graphics.DrawEllipse(pen, rec);
                Font drawFont = new Font("Arial", 18);
                SolidBrush drawBrush = new SolidBrush(Color.Blue);
                StringFormat stringFormat = new StringFormat();
                stringFormat.Alignment = StringAlignment.Center;
                stringFormat.LineAlignment = StringAlignment.Center;

                System.Drawing.Point[] pPoints =
                {
                    new System.Drawing.Point((int)rec.Left, (int)rec.Bottom),      // top left
                    new System.Drawing.Point((int)rec.Right, (int)rec.Top),     // top right
                    new System.Drawing.Point((int)rec.Left, (int)rec.Bottom),   // bottom left
                    new System.Drawing.Point((int)rec.Right, (int)rec.Bottom),  // bottom right
                };
                graphics.TransformPoints(CoordinateSpace.Page, CoordinateSpace.World, pPoints);

                System.Drawing.Drawing2D.Matrix m = graphics.Transform;
                graphics.ResetTransform();
                graphics.DrawString(n.Id, drawFont, drawBrush, pPoints[2].X+20, pPoints[2].Y+20, stringFormat);
                graphics.Transform = m;
            }
            else
            {
                Curve c = curve as Curve;
                foreach (ICurve seg in c.Segs)
                {
                    LineSeg l = seg as LineSeg;
                    if (l != null)
                        graphics.DrawLine(pen, GleePointToDrawingPoint(l.Start), GleePointToDrawingPoint(l.End));
                }
            }
        }
        
        private System.Drawing.Point GleePointToDrawingPoint(Microsoft.Glee.Splines.Point point)
        {
            return new System.Drawing.Point((int)point.X, (int)point.Y);
        }

        private void buttonClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        public void getFOandNodes(string line, ref string[] f, ref int count)
        {
            int size = 0;
            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == ' ')
                    size++;
            }
            f = new string[size + 1];
            for (int i = 0, j = 0; i < line.Length; i++)
            {
                if (line[i] != ' ')
                {
                    f[j] = Convert.ToString(line[i]);
                    count++;
                }
                else j++;
            }
        }

        private void buttonLoad_Click(object sender, EventArgs e)
        {
            Graphics g = this.CreateGraphics();
            int r = 0;
            int count = 0;
            if (textBoxFO1.Text == "" && textBoxFO2.Text == "")
            {
                open.InitialDirectory = "D:\\CData";
                open.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
                open.FilterIndex = 1;
                open.Title = "Відкрити файл";                
                if (open.ShowDialog() == DialogResult.OK)
                {
                    string fname = open.FileName;
                    try
                    {
                        int all = System.IO.File.ReadAllLines(open.FileName).Length;
                        if (all > 0)
                        {
                            StreamReader f = new StreamReader(open.FileName);                            
                            string line;
                            while (!f.EndOfStream)
                            {
                                line = f.ReadLine();
                               
                                if (r == 0)
                                {
                                    textBoxFO1.Text = line;
                                    getFOandNodes(line, ref FO1, ref count);
                                    r = 1;
                                }
                                else if (r == 1)
                                {
                                    textBoxFO2.Text = line;
                                    count = 0;
                                    getFOandNodes(line, ref FO2, ref count);
                                    r = 2;
                                }
                                else if (r == 2)
                                {
                                    textBoxN1.Text = line;
                                    count = 0;
                                    getFOandNodes(line, ref Nodes1, ref count);
                                    if(count > 20)
                                    {
                                        MessageBox.Show("Кількість вершин більша за 20 або ребер білше за 50", "Massage");
                                        return;
                                    }
                                    r = 3;
                                }
                                else
                                {
                                    textBoxN2.Text = line;
                                    count = 0;
                                    getFOandNodes(line, ref Nodes2, ref count);
                                    if (count > 20)
                                    {
                                        MessageBox.Show("Кількість вершин більша за 20 або ребер білше за 50", "Massage");
                                        return;
                                    }
                                }
                            }
                        }
                        else throw new Exception("File empty");
                    }
                    catch
                    {
                        throw new Exception("No file!");
                    }
                }
            }
            else
            {
                getFOandNodes(textBoxFO1.Text, ref FO1, ref count);
                count = 0;
                getFOandNodes(textBoxFO2.Text, ref FO2, ref count);
                count = 0;
                getFOandNodes(textBoxN1.Text, ref Nodes1, ref count);
                if (count > 20)
                {
                    MessageBox.Show("Кількість вершин більша за 20 або кількість ребер більша за 50", "Massage");
                    return;
                }
                count = 0;
                getFOandNodes(textBoxN2.Text, ref Nodes2, ref count);
                if (count > 20)
                {
                    MessageBox.Show("Кількість вершин більша за 20", "Massage");
                    return;
                }
            }

            if (gleeGraph1 == null)
                gleeGraph1 = CreateAndLayoutGraph(Nodes1, FO1);
            textBox1.AppendText("Graph 1 is plotted");

            DrawFromGraph(g, gleeGraph1, 0);

            if (gleeGraph2 == null)
                gleeGraph2 = CreateAndLayoutGraph(Nodes2, FO2);

            DrawFromGraph(g, gleeGraph2, 1);
            textBox1.AppendText("\r\n"+"Graph 2 is plotted");
        }
        
        private void buttonSave_Click(object sender, EventArgs e)
        {
            save.InitialDirectory = @"D:\CData";
            save.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
            save.FilterIndex = 2;
            save.Title = "Зберігти файл як";
            if (save.ShowDialog() == DialogResult.OK)
            {
                if (textBox1.Text != null)
                    try
                    {
                        FileStream fs = new FileStream(save.FileName, FileMode.Create, FileAccess.Write);
                        if (fs != null)
                        {
                            StreamWriter wr = new StreamWriter(fs);
                            wr.WriteLine(textBox1.Text);
                            wr.Flush();
                            wr.Close();
                            fs.Close();
                        }
                    }
                    catch
                    {
                        throw new Exception("File error!");
                    }
                else MessageBox.Show("No Calculate", "Massage");
            }
        }

        private void buttonEuler_Click(object sender, EventArgs e)
        {
            if (gleeGraph1.NodeCount == gleeGraph2.NodeCount)
                textBox1.AppendText("\r\nFor Graps 1 and 2 Number of Nodes is Equal");
            else
            {
                textBox1.AppendText("\r\nFor Graps 1 and 2 Number of Nodes is Unequal");
                textBox1.AppendText("\r\nGraphs 1 and 2 are not Equivalent");
                textBox1.AppendText("\r\nGraphs 1 and 2 are not Equivalent");
                return;
            }

            if (gleeGraph1.EdgeCount == gleeGraph2.EdgeCount)
                textBox1.AppendText("\r\nFor Graps 1 and 2 Number of Edges is Equal");
            else
            {
                textBox1.AppendText("\r\nFor Graps 1 and 2 Number of Edges is Unequal");
                textBox1.AppendText("\r\nGraphs 1 and 2 are not Equivalent");
                return;
            }

            for (int i = 0, j = 0; i < gleeGraph1.NodeCount; i++)
            {
                Node n = gleeGraph1.FindNode(Nodes1[j]);
                if(n.InEdges.Count() != n.OutEdges.Count())
                {
                    textBox1.AppendText("\r\nFor Graph 1 Node " + Nodes1[j] + " Number of In and Out Edges is Unequal");
                    textBox1.AppendText("\r\nGraph 1 does not have Euler Cycle");
                    textBox1.AppendText("\r\nGraphs 1 and 2 are not Equivalent");
                    return;
                }
                j++;
            }
            textBox1.AppendText("\r\nIn Graph 1 all Nodes have Equal Number of In and Out Edges");

            for (int i = 0, j = 0; i < gleeGraph2.NodeCount; i++)
            {
                Node n = gleeGraph2.FindNode(Nodes2[j]);
                if (n.InEdges.Count() != n.OutEdges.Count())
                {
                    textBox1.AppendText("\r\nFor Graph 2 Node " + Nodes2[j] + " Number of In and Out Edges is Unequal");
                    textBox1.AppendText("\r\nGraph 2 does not have Euler Cycle");
                    textBox1.AppendText("\r\nGraphs 1 and 2 are not Equivalent");
                    return;
                }
                j++;
            }
            textBox1.AppendText("\r\nIn Graph 2 all Nodes have Equal Number of In and Out Edges");

            int[] num1 = new int[gleeGraph1.NodeCount];
            int[] num2 = new int[gleeGraph2.NodeCount];
            int num = 0;
            int k = 0;
            for (int i = 0; i < FO1.Length; i++)
            {
                if (i == FO1.Length - 1)
                {
                    num++;
                    num1[k] = num;
                }              
                else if (FO1[i] != "0" )
                    num++;
                else
                {
                    num1[k] = num;
                    num = 0;
                    k++;
                }
            }
            k = 0;
            num = 0;
            for (int i = 0; i < FO2.Length; i++)
            {
                if (i == FO2.Length - 1)
                {
                    num++;
                    num2[k] = num;
                }
                else if (FO2[i] != "0")
                    num++;
                else
                {
                    num2[k] = num;
                    num = 0;
                    k++;
                }
            }
            Array.Sort(num1);
            Array.Sort(num2);
            for(int i = 0; i < num1.Length; i++)
            {
                if(num1[i] != num2[i])
                {
                    textBox1.AppendText("\r\nDegree Sequences for Graphs 1 and 2 are Unequal");
                    textBox1.AppendText("\r\nGraphs 1 and 2 are not Equivalent");
                    return;
                }
            }
            textBox1.AppendText("\r\nDegree Sequences for Graphs 1 and 2 are Equal");

            if (!isEulerianCycle(gleeGraph1, Nodes1))
            {
                textBox1.AppendText("\r\nGraph 1 does not have Euler Cycle");
                return;
            }

            List<string> cycle1 = new List<string>();

            List<Edge> visited = new List<Edge>();
            for (int i = 0; i < gleeGraph1.EdgeCount; i++)
            {
                visited.Add(gleeGraph1.Edges[i]);
            }
            EulerCycle(gleeGraph1.Edges[0], visited, cycle1);
            cycle1.Add(gleeGraph1.Edges[0].Source.Id);

            string s = "\r\nGraph 1 Euler Cycle is { ";
            for (int i = 0; i < cycle1.Count; i++)
                s += cycle1[i] + " ";
            s += "}";
            textBox1.AppendText(s);

            if (!isEulerianCycle(gleeGraph2, Nodes2))
            {
                textBox1.AppendText("\r\nGraph 2 does not have Euler Cycle");
                return;
            }

            List<string> cycle2 = new List<string>();
            visited.Clear();
            for (int i = 0; i < gleeGraph2.EdgeCount; i++)
            {
                visited.Add(gleeGraph2.Edges[i]);
            }
            EulerCycle(gleeGraph2.Edges[0], visited, cycle2);
            cycle2.Add(gleeGraph2.Edges[0].Source.Id);

            s = "\r\nGraph 2 Euler Cycle is { ";
            for (int i = 0; i < cycle2.Count; i++)
                s += cycle2[i] + " ";
            s += "}";
            textBox1.AppendText(s);

            if (!CompareCycles(Nodes1, cycle1, Nodes2, cycle2))
            {
                textBox1.AppendText("\r\nGraphs 1 and 2 are not Equivalent!");
                return;
            }
            textBox1.AppendText("\r\nGraphs 1 and 2 are Equivalent!");
        }

        private void FormGr_Paint(object sender, PaintEventArgs e)
        {
            if (gleeGraph1 != null)
                DrawFromGraph(e.Graphics, gleeGraph1, 0);
            if (gleeGraph2 != null)
                DrawFromGraph(e.Graphics, gleeGraph2, 1);

        }

        private GleeGraph CreateAndLayoutGraph(string[] nodes, string[] FO)
        {
            double w = 50;
            double h = 50;
 
            GleeGraph g = new GleeGraph();
            for (int i = 0; i < nodes.Length; i++)
            {
                g.AddNode(new Node(nodes[i], new Ellipse(w, h, new P())));
            }

            for (int i = 0, j = 0; i < FO.Length; i++)
            {
                if (FO[i] != "0")
                {
                    Node a = g.FindNode(nodes[j]);
                    Node b = g.FindNode(FO[i]);
                    Edge e = new Edge(a, b);
                    e.ArrowHeadAtTarget = true;
                    e.ArrowHeadLength = 30;
                    g.AddEdge(e);
                }
                else j++;
            }
            g.CalculateLayout();
            return g;
        }
    }
    public static partial class Extensions
    {
        public static bool IsAlpha(this string @this)
        {
            return !Regex.IsMatch(@this, "[^a-zA-Z]");
        }
    }
}
