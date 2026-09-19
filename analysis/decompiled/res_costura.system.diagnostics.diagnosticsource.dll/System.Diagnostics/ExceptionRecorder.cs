namespace System.Diagnostics;

public delegate void ExceptionRecorder(Activity activity, Exception exception, ref TagList tags);
