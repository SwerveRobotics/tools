# Swerve Robotics Tools Suite

Welcome to the Swerve Robotics Tools Suite. Here you will find tools that we hope will
make your FTC programming more productive and enjoyable. You might also enjoy our
related project, the Swerve Robotics FTC Library, also here on GitHub.

At the moment, the suite contains just one tool:

*   __BotBug__: BotBug helps you debug your robot over Wifi by automatically configuring 
    robot controller phones for Wifi debugging whenever they connect to your PC using USB.

    Wifi debugging is especially useful for FTC robots, as it supports live Android Studio 
    debugging and downloading of updates of a team's software even while the robot is in 
    its game configuration, connected to its sensors and motor and servo controllers. Configuring 
    Wifi debugging by hand can be straightforward but tedious, and its several command-line steps
    must be done (at least) each time a phone boots or Android Studio restarts.

    The first thing to do is to connect your PC and your robot controller over a common wireless
    network, which is described below. Once your PC and your robot controller are connected on a 
    wireless network, just plug your phone into your PC using USB, acknowledge the one-time 
    "allow USB debugging" prompt if needed, and BotBug will take care of the rest. Once 
    BotBug has done its job, you can detach the phone from USB.

    There are two approaches to connecting to your robot controller over Wifi. The preferred 
    approach is to use the same Wifi Direct network that the driver station uses to talk to the
    controller. The second approach is to connect to the robot controller over an administered
    Wifi network (one with a regular access point) which is also visible to your PC.

    The private Wifi Direct network hosted by your robot controller will appear to your PC as
    a wireless network whose name/SSID is of the form 'DIRECT-TwoRandomCharacters-*YourRobotName*',
    for example: 'DIRECT-S6-1234-RC'. (this network might not be visible when the FTC 
    Robot Controller application is not running on the phone). You can connect to it from 
    your PC like you can connect to any other wireless network so long as you know the password. 
    One way to learn the password is to write your code using the [Swerve Library](https://github.com/SwerveRobotics/ftc_app) 
    in which case the password will be displayed on the robot controller screen (it has also been
    mentioned that a future release from FTC HQ may also display the password on the robot 
    controller screen). A second way is to look for the string 'PassPhrase' in the Android 
    Studio LogCat output from the robot controller.

    You can of course connect over Wifi Direct to your robot controller using your usual wireless
    network adapter in your laptop or desktop. However, it's often useful to have your PC connect 
    *both* to your robot controller and *also* to the Internet, and that will require a second 
    network adapter. If your PC doesn't already have a second adapter, USB wireless network adapters
    are readily and inexpensively available from sources such as [Amazon](http://www.amazon.com/Edimax-EW-7811Un-150Mbps-Raspberry-Supports/dp/B003MTTJOY/ref=sr_1_2).

    If you're not going to connect over the Wifi Direct network, configure your robot controller 
    phone normally, just as instructed in the FTC Guide, then just take the one additional 
    step of also connecting the phone to a regular Wifi network, one which is visible to your PC 
    (so, in this second case, the robot controller is *both* on Wifi Direct and this other network).
    
    BotBug automatically starts after install and restarts when your computer boots. It is 
    always running in the background, but can be temporarily disarmed by using the menu on 
    the Swerve icon in the notification area.

    Sorry, BotBug only runs on Windows (v7 or greater), and we don't have the knowledge 
    or resources ourselves to port it to other platforms, though we would support anyone
    who might choose to undertake such a port.

To install the Swerve Robotics Tools Suite, download a release from here on GitHub (look at
the top of this page just above the thick green line), then run the downloaded file.
We hope you find these tools to be useful. We'd love to hear what you think, and we will respond to
questions or issues you have as promptly as we are able.

Robert Atkinson,  
bob@theatkinsons.org,  
Mentor, Swerve Robotics,    
Woodinville, Washington

