kinect-pong
===========

An implementation of Pong in a C# WPF application, using the Microsoft Kinect for controls.    

Uses [WpfKinectHelper] (https://github.com/bencentra/WpfKinectHelper) for interfacing with the Kinect. 

Made by [Ben Centra](https://github.com/bencentra).   

How To Play
-----------

It's Pong; try and bounce the ball past your opponent's paddle without letting the ball get past yours.
**Controls:**    
1) Select the handedness of each player in the options on the left.    
2) Stand in front of the Kinect. The player on the left is Player One (blue), the player on the right is
Player Two (red).    
3) Raise and lower your selected hand to move your paddle and block the ball.

How It Works
------------

Skeleton detection:

	// Event handler for changes in Skeleton stream data
    // Used as a secondary "update" method for setting the position of kinect-controlled paddles
    private void SkeletonDataChanged(object sender, SkeletonDataChangeEventArgs e) {
        ...
        // Determine which skeletons are on which side
        Skeleton right = null;
        Skeleton left = null;
        // Loop through all available skeletons
        for (int i = 0; i < e.skeletons.Length; i++) {
            // Grab the current skeleton
            Skeleton skel = e.skeletons[i];
            // If we're tracked figure out what side of the screen we're on
            if (skel.TrackingState == SkeletonTrackingState.Tracked) {
                Point position = helper.SkeletonPointToScreen(skel.Joints[JointType.ShoulderCenter].Position);
                // If the skeleton is the first on the left side of the screen, it is the left skeleton
                if ((position.X > 0 && position.X <= GameCanvas.Width / 2) && left == null)
                    left = skel;
                // If the skeleton is the first on the right side of the screen, it is the right skeleton
                else if ((position.X > GameCanvas.Width / 2 && position.X < GameCanvas.Width) && right == null)
                    right = skel;
            }
            // If both skeletons have been found, no need to keep looking
            if (left != null & right != null)
                break;
        }
        ...
        // Get the locations of the skeleton's hand
        Point playerOneHand = helper.SkeletonPointToScreen(left.Joints[playerOneHandedness].Position);
        ...
        // Update the position of player one's paddle
        playerOnePos.Y = playerOneHand.Y - playerOne.Height / 2;
        ...
    }

Future Improvements
-------------------
* Further fine-tuning for collisions, angle randomization, etc.    
* Single player mode.    