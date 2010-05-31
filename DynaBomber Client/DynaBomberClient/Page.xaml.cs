using System;
using System.Windows.Media;
using DynaBomberClient.GameOver;
using DynaBomberClient.MainGame;
using DynaBomberClient.MainMenu;

namespace DynaBomberClient
{
    public partial class Page : System.Windows.Controls.Page
    {
        public enum GameStates
        {
            Menu,
            Main,
            GameOver
        }

        private IGameState _currentState;

        public Page()
        {
            InitializeComponent();
            CompositionTarget.Rendering += CompositionTarget_Rendering;
            
            // Create the menu at the beginning
            _currentState = new MainMenuState(this);
            _currentState.Activate();
        }

        private void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            _currentState.EnterFrame(0);
        }

        public IGameState ActiveState
        {
            get { return _currentState; }
            set
            {
                _currentState.Deactivate();
                _currentState = value;
                _currentState.Activate();
            }
        }
    }
}
