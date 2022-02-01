using McMaster.Extensions.CommandLineUtils;
using NonlinearFilters.CLI;

Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
return CommandLineApplication.Execute<Startup>(args);
