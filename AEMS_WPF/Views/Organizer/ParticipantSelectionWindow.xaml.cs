using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BusinessLogic.DTOs.Role.Organizer;

namespace AEMS_WPF.Views.Organizer
{
    public partial class ParticipantSelectionWindow : Window
    {
        private List<EventParticipantDto> _allParticipants;
        public EventParticipantDto? SelectedParticipant { get; private set; }

        public ParticipantSelectionWindow(List<EventParticipantDto> participants)
        {
            InitializeComponent();
            _allParticipants = participants;
            lbParticipants.ItemsSource = _allParticipants;
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            string search = txtSearch.Text.ToLower();
            lbParticipants.ItemsSource = _allParticipants.Where(p => 
                p.FullName.ToLower().Contains(search) || 
                p.StudentCode.ToLower().Contains(search)).ToList();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnSelect_Click(object sender, RoutedEventArgs e)
        {
            if (lbParticipants.SelectedItem is EventParticipantDto participant)
            {
                SelectedParticipant = participant;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Please select a participant.");
            }
        }
    }
}
