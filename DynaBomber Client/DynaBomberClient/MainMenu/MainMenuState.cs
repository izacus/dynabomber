using System.Windows;
using System.Windows.Input;
using DynaBomberClient.MainGame;

namespace DynaBomberClient.MainMenu
{
    public class MainMenuState : IGameState
    {
        private Page _page;

        public MainMenuState(Page page)
        {
            _page = page;

            _page.menuText.Visibility = Visibility.Visible;
            _page.gameTitle.Visibility = Visibility.Visible;

            _page.KeyUp += StartGame;
        }

        private void StartGame(object sender, KeyEventArgs e)
        {
            _page.KeyUp -= StartGame;
            _page.ActiveState = new MainGameState(_page.GameArea);
        }

        public void EnterFrame(double dt)
        {
            // Nothing TBD
        }

        public void Deactivate()
        {
            _page.menuText.Visibility = Visibility.Collapsed;
            _page.gameTitle.Visibility = Visibility.Collapsed;
        }
    }
}
