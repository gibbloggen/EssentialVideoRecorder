using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace EssentialVideoRecorder
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class Help : Page
	{
		public Help()
		{
			this.InitializeComponent();

		

			Window.Current.SizeChanged += Current_SizeChanged;



			Uri j = new Uri("http://www.essentialsoftwareproducts.org/essential-video-recorder/");
			HelpView.Navigate(j);
			HelpView.Visibility = Visibility.Visible;
			HelpView.Width = Window.Current.Bounds.Width;
			HelpView.Height = Window.Current.Bounds.Height - 80;
			/*	Window.Current.Closed += (ss, ee) =>
				{
					Frame.Navigate(typeof(MainPage));
				};
				*/


		}

	

		private void BackToProgram_Tapped(object sender, TappedRoutedEventArgs e)
		{
			Frame.Navigate(typeof(MainPage));


		}

		private void Current_SizeChanged(object sender, Windows.UI.Core.WindowSizeChangedEventArgs e)
		{


			HelpView.Width = e.Size.Width;
			HelpView.Height = e.Size.Height - 80;
			// GetTheVideo.Width = e.Size.Width;
			//GetTheVideo.Height = e.Size.Height;


		}

		private void Current_Closed(object sender, EventArgs e)
		{

			Frame.Navigate(typeof(MainPage));
		}
		}
	}
