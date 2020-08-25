﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace ArcNavDemo.Auth
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LoginPage : ContentPage
    {
        // Event to provide login information when the user dismisses the view.
        public event EventHandler<LoginEventArgs> OnLoginInfoEntered;
        // Event to report that the login was canceled.
        public event EventHandler OnCanceled;
        public LoginPage()
        {
            InitializeComponent();
            UsernameEntry.Text = "jay.bowman_DEV_PRO";
            this.PasswordEntry.Text = "jB1rd@35r!";
        }
        // Text to display at the top of the login controls.
        public string TitleText
        {
            set
            {
                LoginLabel.Text = value;
            }
        }
        private void LoginButtonClicked(object sender, EventArgs e)
        {
            // Get the values entered in the text fields.
            string username = UsernameEntry.Text;
            string password = PasswordEntry.Text;

            // Make sure the user entered all values.
            if (String.IsNullOrEmpty(username) || String.IsNullOrEmpty(password))
            {
                DisplayAlert("Login", "Please enter a username and password", "OK");
                return;
            }

            // Fire the OnLoginInfoEntered event and provide the login values.
            if (OnLoginInfoEntered != null)
            {
                // Create a new LoginEventArgs to contain the user's values.
                LoginEventArgs loginEventArgs = new LoginEventArgs(username.Trim(), password.Trim());

                // Raise the event.
                OnLoginInfoEntered(sender, loginEventArgs);
            }
        }

        private void CancelButtonClicked(object sender, EventArgs e)
        {
            // Fire the OnCanceled event to let the calling code no the login was canceled.
            if (OnCanceled != null)
            {
                OnCanceled(this, null);
            }
        }
    }

    // Custom EventArgs implementation to hold login information (username and password).
    public class LoginEventArgs : EventArgs
    {
        // Username property.
        public string Username { get; set; }

        // Password property.
        public string Password { get; set; }

        // Store login values passed into the constructor.
        public LoginEventArgs(string username, string password)
        {
            Username = username;
            Password = password;
        }
    }

}