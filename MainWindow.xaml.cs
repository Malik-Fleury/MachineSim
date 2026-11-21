using System.Windows;
using MachineSim.Hardware;
using MachineSim.ViewModels;

namespace MachineSim;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Point de composition : c'est le SEUL endroit qui choisit l'implémentation de IMachine.
        // Demain, une vraie machine (driver CN, OPC UA...) pourrait remplacer SimulatedMachine
        // sans toucher une ligne du ViewModel. C'est tout l'intérêt de l'interface.
        //
        // Le code-behind s'arrête là : pas de logique ici, tout passe par le ViewModel.
        DataContext = new MainViewModel(new SimulatedMachine());
    }
}
