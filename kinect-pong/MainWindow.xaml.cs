using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Kinect;

namespace kinect_pong
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Constants
        private const double DEG_TO_RAD = Math.PI / 180;    // Converting degrees to radians
        private const double MARGIN_OFFSET = 25;            // Offset of paddles from edges of canvas
        private const int FRAME_RATE = 60;                  // Frame rate for the animation

        // Instance of Kinect Helper, access to data streams and sensor properties
        private KinectHelper helper;

        // Random number generator
        private Random rando;

        // Thread to run all game operations on
        private Thread gameThread;
        private bool gamePaused = false;
        
        // Player paddles and positions
        private Rectangle playerOne;
        private Rectangle playerTwo;
        private Point playerOnePos;
        private Point playerOnePosPast;
        private Point playerTwoPos;
        private Point playerTwoPosPast;
 
        // Ball and it's movement/location properties
        private Ellipse ball;
        private Point ballPos;
        private Vector ballFwd;
        private double ballSpd;

        // Markers for the player skeletons
        private Ellipse markOne;
        private Ellipse markTwo;
        private Point markOnePos;
        private Point markTwoPos;

        // Scorekeepers
        private int playerOneScore = 0;
        private int playerTwoScore = 0;

        public MainWindow()
        {
            InitializeComponent();
            // Start the game
            rando = new Random();
            playerOnePos = new Point();
            playerOnePosPast = new Point();
            playerTwoPos = new Point();
            playerTwoPosPast = new Point();
            ballPos = new Point();
            ballFwd = new Vector();
            markOnePos = new Point();
            markTwoPos = new Point();
            InitializeGame();
            //Initialize the Kinect Helper for Skeleton data only
            helper = new KinectHelper(false, false, true);
            helper.ToggleSeatedMode(true);
            helper.SkeletonDataChanged += this.SkeletonDataChanged;
            SkeletonImage.Source = helper.skeletonBitmap;
        }

        public void InitializeGame()
        {
            Console.WriteLine("Canvas Width: " + GameCanvas.Width + " Height: " + GameCanvas.Height);
            // Initialize the ball
            ball = new Ellipse();
            ball.Width = 25;
            ball.Height = 25;
            ball.Stroke = new SolidColorBrush(Colors.Black);
            ball.StrokeThickness = 2;
            ball.Fill = new SolidColorBrush(Colors.Green);
            ResetBall();
            GameCanvas.Children.Add(ball);
            Console.WriteLine("Ball Fwd X: " + ballFwd.X + " Y: " + ballFwd.Y);
            // Initialize the left paddle (Player One)
            playerOne = new Rectangle();
            playerOne.Width = 40;
            playerOne.Height = 120;
            playerOne.Stroke = new SolidColorBrush(Colors.Black);
            playerOne.StrokeThickness = 2;
            playerOne.Fill = new SolidColorBrush(Colors.Blue);
            playerOnePos.X = MARGIN_OFFSET;
            playerOnePos.Y = GameCanvas.Height / 2 - playerOne.Height / 2;
            GameCanvas.Children.Add(playerOne);
            // Initialize the right paddle (Player Two)
            playerTwo = new Rectangle();
            playerTwo.Width = 40;
            playerTwo.Height = 120;
            playerTwo.Stroke = new SolidColorBrush(Colors.Black);
            playerTwo.StrokeThickness = 2;
            playerTwo.Fill = new SolidColorBrush(Colors.Red);
            playerTwoPos.X = GameCanvas.Width - playerTwo.Width - MARGIN_OFFSET;
            playerTwoPos.Y = GameCanvas.Height / 2 - playerTwo.Height / 2;
            GameCanvas.Children.Add(playerTwo);
            // Initialize player markers
            markOne = new Ellipse();
            markOne.Width = 75;
            markOne.Height = 75;
            markOne.Fill = new SolidColorBrush(Colors.Blue);
            markOne.Opacity = 0;
            markOnePos.X = GameCanvas.Width;
            markOnePos.Y = GameCanvas.Height;
            GameCanvas.Children.Add(markOne);
            markTwo = new Ellipse();
            markTwo.Width = 75;
            markTwo.Height = 75;
            markTwo.Fill = new SolidColorBrush(Colors.Red);
            markTwo.Opacity = 0;
            markTwoPos.X = GameCanvas.Width;
            markTwoPos.Y = GameCanvas.Height;
            GameCanvas.Children.Add(markTwo);
            // Initialize the game thread
            gameThread = new Thread(new ThreadStart(this.GameLoop));
            gameThread.IsBackground = true;
            gameThread.Start();
        }

        public void gameTimer_Tick(object sender, EventArgs e)
        {
            Console.WriteLine("TICK");
        }

        public void GameLoop()
        {
            while (!this.gamePaused)
            {
                Thread.Sleep(1000 / FRAME_RATE);
                this.Dispatcher.Invoke((Action)(() =>
                {
                    Update();
                    Draw();
                }));  
            }
        }

        public void Update()
        {
            // Move the ball forward
            ballPos = Vector.Add(ballFwd * ballSpd, ballPos);

            // Bounce off the side walls
            if (ballPos.Y < 0 || ballPos.Y > GameCanvas.Height - ball.Height)
            {
                BounceBallVertical();   
            }

            // If the ball hit the left paddle (Player One), make it bounce
            if (ballPos.X < MARGIN_OFFSET + playerOne.Width)
            {
                if (ballPos.Y < playerOnePos.Y + playerOne.Height + ball.Height/2 && ballPos.Y > playerOnePos.Y - ball.Height/2)
                {
                    ballPos.X = playerOnePos.X + playerOne.Width + 3;
                    BounceBallHorizontal();
                }
                else if (ballPos.X < MARGIN_OFFSET + playerOne.Width / 2)
                {
                    ResetBall();
                    playerTwoScore += 1;
                    scoreTwoLabel.Content = playerTwoScore;
                }
            }
            
            // Check if the ball will score on player two
            if (ballPos.X > GameCanvas.Width - MARGIN_OFFSET - playerTwo.Width - ball.Width)
            {
                // If the ball hit the paddle, make it bounce
                if (ballPos.Y < playerTwoPos.Y + playerTwo.Height && ballPos.Y > playerOnePos.Y)
                {
                    ballPos.X = playerTwoPos.X - ball.Width - 3;
                    BounceBallHorizontal();
                }
                else if (ballPos.X > GameCanvas.Width - MARGIN_OFFSET - playerTwo.Width / 2 - ball.Width)
                {
                    ResetBall();
                    playerOneScore += 1;
                    scoreOneLabel.Content = playerOneScore;
                }
            }
            
        }

        public void ResetBall()
        {
            ballPos = new Point(GameCanvas.Width / 2 - ball.Width / 2, GameCanvas.Height / 2 - ball.Height / 2);
            ballSpd = 4;
            double angleX = rando.NextDouble() * 135 + 135;
            double angleY = rando.NextDouble() * 135 + 135;
            ballFwd = new Vector(Math.Sin(angleX * DEG_TO_RAD), Math.Cos(angleY * DEG_TO_RAD));
        }

        public void BounceBallHorizontal()
        {
            ballFwd.X = -ballFwd.X;
            ballSpd += .5;
        }

        public void BounceBallVertical()
        {
            ballFwd.Y = -ballFwd.Y;
        }

        public void Draw()
        {
            // Move the paddles to their new positions
            Canvas.SetLeft(playerOne, playerOnePos.X);
            Canvas.SetTop(playerOne, playerOnePos.Y);
            Canvas.SetLeft(playerTwo, playerTwoPos.X);
            Canvas.SetTop(playerTwo, playerTwoPos.Y);

            // Move the markers to their new positions
            Canvas.SetLeft(markOne, markOnePos.X);
            Canvas.SetTop(markOne, markOnePos.Y);
            Canvas.SetLeft(markTwo, markTwoPos.X);
            Canvas.SetTop(markTwo, markTwoPos.Y);

            // Move the ball to its new position
            Canvas.SetLeft(ball, ballPos.X);
            Canvas.SetTop(ball, ballPos.Y);
        }

        // Event handler for changes in Skeleton stream data
        // Used as an "update" method for any kinect-controlled paddles.
        public void SkeletonDataChanged(object sender, SkeletonDataChangeEventArgs e)
        {
            // Determine which skeletons are on which side
            Skeleton right = null;
            Skeleton left = null;
            for (int i = 0; i < e.skeletons.Length; i++)
            {
                Skeleton skel = e.skeletons[i];
                if (skel.TrackingState == SkeletonTrackingState.Tracked)
                {
                    Point position = helper.SkeletonPointToScreen(skel.Joints[JointType.ShoulderCenter].Position);
                    if ((position.X > 0 && position.X <= GameCanvas.Width / 2) && left == null)
                        left = skel;
                    else if ((position.X > GameCanvas.Width / 2 && position.X < GameCanvas.Width) && right == null)
                        right = skel;
                }
                if (left != null & right != null)
                    break;
            }

            // Update the position of the left skeleton (Player One)
            if (left == null)
            {
                markOne.Opacity = 0;
                markOnePos.X = GameCanvas.Width;
                markOnePos.Y = GameCanvas.Height;
            }
            else
            {
                Point playerOneHand = helper.SkeletonPointToScreen(left.Joints[JointType.HandRight].Position);
                Point playerOneHead = helper.SkeletonPointToScreen(left.Joints[JointType.Head].Position);
                playerOnePosPast = playerOnePos;
                playerOnePos.Y = playerOneHand.Y - playerOne.Height / 2;
                markOne.Opacity = 1;
                markOnePos.X = playerOneHead.X - markOne.Width / 2;
                markOnePos.Y = playerOneHead.Y - markOne.Height / 2;
            }

            // Update the position of the right skeleton (Player Two);
            if (right == null)
            {
                markTwo.Opacity = 0;
                markTwoPos.X = GameCanvas.Width;
                markTwoPos.Y = GameCanvas.Height;
            }
            else
            {
                Point playerTwoHand = helper.SkeletonPointToScreen(right.Joints[JointType.HandRight].Position);
                Point playerTwoHead = helper.SkeletonPointToScreen(right.Joints[JointType.Head].Position);
                playerTwoPosPast = playerTwoPos;
                playerTwoPos.Y = playerTwoHand.Y - playerTwo.Height / 2;
                markTwo.Opacity = 1;
                markTwoPos.X = playerTwoHead.X - markTwo.Width / 2;
                markTwoPos.Y = playerTwoHead.Y - markTwo.Height / 2;
            }
        }

        // Event handler for closing the window
        // Used to abort the game thread
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            gameThread.Abort();
        }
    }
}
