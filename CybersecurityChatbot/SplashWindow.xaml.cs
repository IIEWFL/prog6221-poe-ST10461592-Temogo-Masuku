using System;
using System.Media;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace CybersecurityChatbot
{
    public partial class SplashWindow : Window
    {
        private DispatcherTimer animationTimer;
        private DispatcherTimer closeTimer;
        private int dotCount = 0;

        public SplashWindow()
        {
            InitializeComponent();
            ShowLogo();
            PlayWelcomeSound();
            StartLoadingAnimation();
            StartCloseTimer();

            // Handle ESC key to close
            this.KeyDown += SplashWindow_KeyDown;
        }

        private void ShowLogo()
        {
            // Your original ASCII logo - optimized for full screen display
            string logo = @"    
                                   CYBERSECURITY AWERENESS CHATBOT.....++...                             .                
              .                          .....:+****+:.....                                         
            ..                           ...-****++****-...             .   .      .  .             
. .             .         .   .      ....:+***++=++=++***+:...                .                     
           .             .    ........-+***++=+++**+==+++***+-........              . .             
                              ...:-+****+++++++%%%%%#++=++++****+-:...                              
.        .  .     .   .  ..=++******+++=+==+#%%%%%%%%%##+==+++++*******+=...            . .         
.                     .  ..+**++++=+=+++*#%%%%%%%%%%%%%%%%#*+=====+++***+...               . .. .   
                       . ..+**+==+++#%%%%%%##############%%%%%%*+++++***+...  .            .        
     .                   ..+**+==*%%%%%%########################%%%%+++***+...        . .             
.    .                  ...+**+==*%%%%########################%%%%+++***+...   .        .     .     
         .     .         ..+**+==*%%#############**############%%%+++***+...            . .         
   .      .          .  ...+**+==*#%##########*::*+::*###########%+=+***+...          .             
.  .           .         ..+**+==*###########*-+*****-*###########+=+***+...                        
 .       .   . .         ..+**+==*##########**:******:**##########+=+***+...  .        .  .   .  .  
                  .      ..+**+==*#########**.:::::::::**#########+++#**+....      .        .     . 
   .               .     ..+**+==*#########*=..........=*#########+++#**+...               .   .    
       .                 ..+**+==*#########*=....==....=*#########+++#**+...                   .    
 .                    .  ..+**+==*##########+....++....=*#########+=+#**+...           .        .   
             .           ..=**+=++%#########+....+*....=#########%==+#**=...                  ..    
.  .                     ..:***+++*#########+..........+#########++=+#**:..                      .  
                      .  ...+**+=++#############################*+==%#*+...  .                   .  
   .. .  .               . .:***+==+#%#########################*+=+##*+:. .              .          
 .     .              .    ..-***+=+=*########################++==##**-..    .         .            
                 .        ....-****+==+##%################%#*++=+%***-...  .                   .    
     .     .       .      ......+***+=+=+#%%%%########%%#%#+=++##**+..   .              .       .   
 .    .                       ...=****+=+++#%%%%%%%%%%%%*+=++*#**+=...    .  .       .              
      .  .                    .....=****+++=++%%%%%%%#+++=+##***=......      .                  .   
      .    .     .          . .  ....=****+++==+*##*+==+*##***=.....       .           .            
                     .              ...=*****++====+++##****=....                         .         
    .                             . .....:+*****+++******+:.....  .                                 
    .             .                    .....:+********+:.....      .           .. .                 
          . .                               ....=++=.....         ..        .     .  .  .           ";

            LogoTextBlock.Text = logo;
        }

        private void PlayWelcomeSound()
        {
            try
            {
                // Play a simple welcome sound
                SystemSounds.Asterisk.Play();
            }
            catch (Exception)
            {
                // Silent fail for sound issues
            }
        }

        private void StartLoadingAnimation()
        {
            // Animate the loading dots
            animationTimer = new DispatcherTimer();
            animationTimer.Interval = TimeSpan.FromMilliseconds(600);
            animationTimer.Tick += AnimationTimer_Tick;
            animationTimer.Start();
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            dotCount = (dotCount + 1) % 4;
            DotsTextBlock.Text = new string('.', dotCount + 1);

            // Original loading messages
            string[] messages = {
                "Initializing cybersecurity protocols",
                "Loading task management system",
                "Setting up intelligent reminders",
                "Preparing chatbot intelligence",
                "Activating security awareness mode",
                "Ready to protect you online"
            };

            int messageIndex = (dotCount) % messages.Length;
            LoadingTextBlock.Text = messages[messageIndex];
        }

        private void StartCloseTimer()
        {
            // Auto-close after 4 seconds (same as original App.xaml.cs timing)
            closeTimer = new DispatcherTimer();
            closeTimer.Interval = TimeSpan.FromSeconds(4);
            closeTimer.Tick += CloseTimer_Tick;
            closeTimer.Start();
        }

        private void CloseTimer_Tick(object sender, EventArgs e)
        {
            CloseSplash();
        }

        private void SplashWindow_KeyDown(object sender, KeyEventArgs e)
        {
            // Allow any key to close
            CloseSplash();
        }

        private void CloseSplash()
        {
            closeTimer?.Stop();
            animationTimer?.Stop();

            this.Close();
        }
    }
}