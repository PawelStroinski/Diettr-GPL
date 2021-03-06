// Ideas from http://stackoverflow.com/a/10358260 & https://github.com/brianchance/MvvmCross-UserInteraction/blob/641279a519970e16a8de186930d878a0e1185a69/Chance.MvvmCross.Plugins.UserInteraction.Droid/UserInteraction.cs
using System;
using System.Threading;
using Android.App;
using Android.OS;
using Android.Text;
using Android.Views;
using Android.Widget;
using Java.Lang;
using MvvmCross.Platform;
using MvvmCross.Platform.Droid.Platform;

namespace Dietphone.Tools
{
    public class MessageDialogImpl : MessageDialog
    {
        private static readonly Handler breakLoop = new Handler(delegate { throw new BreakLoopException(); });

        public void Show(string text)
        {
            if (IsUiThread())
            {
                ShowDo(text);
            }
            else
            {
                PostToUiThread(() =>
                {
                    ShowDo(text);
                });
            }
        }

        public void Show(string text, string caption)
        {
            if (IsUiThread())
            {
                ShowDo(text, caption);
            }
            else
            {
                PostToUiThread(() =>
                {
                    ShowDo(text, caption);
                });
            }
        }

        public bool Confirm(string text, string caption)
        {
            if (IsUiThread())
            {
                return ConfirmDo(text, caption);
            }
            else
            {
                return DispatchedConfirm(text, caption);
            }
        }

        public string Input(string text, string caption)
        {
            if (IsUiThread())
            {
                return InputDo(text, caption);
            }
            else
            {
                return DispatchedInput(text, caption);
            }
        }

        public string Input(string text, string caption, string value)
        {
            if (IsUiThread())
            {
                return InputDo(text, caption, value);
            }
            else
            {
                return DispatchedInput(text, caption, value);
            }
        }

        public string Input(string text, string caption, string value, InputType type)
        {
            if (IsUiThread())
            {
                return InputDo(text, caption, value, type);
            }
            else
            {
                return DispatchedInput(text, caption, value, type);
            }
        }

        private bool IsUiThread()
        {
            var looper = Looper.MainLooper;
            return looper.IsCurrentThread;
        }

        private void PostToUiThread(Action action)
        {
            var synchronizationContext = Application.SynchronizationContext;
            synchronizationContext.Post(_ => action(), null);
        }

        private bool DispatchedConfirm(string text, string caption)
        {
            var signal = new ManualResetEvent(false);
            var confirmed = false;
            PostToUiThread(() =>
            {
                confirmed = ConfirmDo(text, caption);
                signal.Set();
            });
            signal.WaitOne();
            return confirmed;
        }

        private string DispatchedInput(string text, string caption, string value = null,
            InputType type = InputType.Default)
        {
            var signal = new ManualResetEvent(false);
            string input = null;
            PostToUiThread(() =>
            {
                input = InputDo(text, caption, value, type);
                signal.Set();
            });
            signal.WaitOne();
            return input;
        }

        private void ShowDo(string text, string caption = null)
        {
            var activity = GetActivity();
            var builder = new AlertDialog.Builder(activity)
                .SetMessage(text)
                .SetNeutralButton(GetOk(activity), delegate { });
            if (caption != null)
                builder.SetTitle(caption.Capitalize());
            ShowAndWait(builder);
        }

        private bool ConfirmDo(string text, string caption)
        {
            var activity = GetActivity();
            var ok = false;
            var builder = new AlertDialog.Builder(activity)
                .SetMessage(text)
                .SetTitle(caption.Capitalize())
                .SetPositiveButton(GetOk(activity), delegate { ok = true; })
                .SetNegativeButton(GetCancel(activity), delegate { })
                .SetCancelable(false);
            ShowAndWait(builder);
            return ok;
        }

        private string InputDo(string text, string caption, string value = null, InputType type = InputType.Default)
        {
            var activity = GetActivity();
            var ok = false;
            var input = new EditText(activity) { Text = value ?? string.Empty };
            ConfigureInput(input, type);
            var builder = new AlertDialog.Builder(activity)
                .SetMessage(text)
                .SetTitle(caption.Capitalize())
                .SetPositiveButton(GetOk(activity), delegate { ok = true; })
                .SetNegativeButton(GetCancel(activity), delegate { })
                .SetCancelable(false)
                .SetView(input);
            ShowAndWait(builder, softInput: true);
            if (ok)
                return string.IsNullOrEmpty(input.Text) ? input.Text : input.Text.TrimEnd(' ');
            else
                return null;
        }

        private Activity GetActivity()
        {
            var holder = Mvx.Resolve<IMvxAndroidCurrentTopActivity>();
            var activity = holder.Activity;
            if (activity == null)
                throw new InvalidOperationException("Current top activity is not available");
            return activity;
        }

        private string GetOk(Activity activity)
        {
            return activity.GetString(Android.Resource.String.Ok);
        }

        private string GetCancel(Activity activity)
        {
            return activity.GetString(Android.Resource.String.Cancel);
        }

        private void ShowAndWait(AlertDialog.Builder builder, bool softInput = false)
        {
            var dialog = builder.Create();
            dialog.DismissEvent += delegate { breakLoop.SendMessage(breakLoop.ObtainMessage()); };
            if (softInput)
            {
                var window = dialog.Window;
                window.SetSoftInputMode(SoftInput.StateVisible);
            }
            dialog.Show();
            Loop();
        }

        private void ConfigureInput(EditText input, InputType type)
        {
            input.InputType = GetInputTypes(type);
            var text = input.Text;
            if (!string.IsNullOrEmpty(text))
                input.SetSelection(text.Length);
            input.SetSingleLine(true);
        }

        private void Loop()
        {
            try
            {
                Looper.Loop();
            }
            catch (BreakLoopException)
            {
            }
        }

        private InputTypes GetInputTypes(InputType type)
        {
            switch (type)
            {
                case InputType.Email:
                    return InputTypes.ClassText | InputTypes.TextVariationEmailAddress;
                case InputType.Url:
                    return InputTypes.ClassText | InputTypes.TextVariationWebEmailAddress;
                case InputType.Default:
                    return InputTypes.ClassText | InputTypes.TextFlagCapSentences;
            }
            throw new ArgumentOutOfRangeException();
        }
    }

    public class BreakLoopException : RuntimeException
    {
    }
}