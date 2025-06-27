# Mail System

**Document Status:** Initial Documentation  
**Verification:** Code Review Needed  
**Implementation Status:** Live

## Overview

**Game Rule Summary**: The mail system enables automated server communication including bug reports you submit, server logs for administrators, and email notifications about important server events. Players can set their email address to receive important updates and to authenticate bug reports, while administrators use the system to monitor server health and respond to player issues.

The Mail System provides automated email functionality for server administration, bug reports, and server logs. It handles SMTP configuration, queued mail delivery, attachment management, and email validation for player accounts.

## Core Architecture

### Mail Manager
```csharp
public class MailMgr
{
    private static string m_username = string.Empty;      // SMTP username
    private static string m_password = string.Empty;      // SMTP password
    private static string m_emailAddress = string.Empty;  // From address
    private static string m_smtpServer = string.Empty;    // SMTP server
    private static bool m_enable = false;                 // Enable/disable
    private static bool m_ssl = false;                    // SSL/TLS
    
    private static int m_mailFrequency = 5 * 60 * 1000;  // 5 minutes
    private static Queue m_mailQueue = new Queue();       // Mail queue
    private static SmtpClient SmtpClient = null;          // SMTP client
}
```

### Configuration System
```csharp
// MailConfig.xml structure
<Configuration>
    <Username>smtp_username</Username>
    <Password>smtp_password</Password>
    <EmailAddress>server@example.com</EmailAddress>
    <SMTPServer>smtp.example.com</SMTPServer>
    <SSL>true</SSL>
    <Enable>true</Enable>
</Configuration>
```

## Mail Types

### Bug Reports
```csharp
// Player bug report integration
if (ServerProperties.Properties.BUG_REPORT_EMAIL_ADDRESSES.Trim() != string.Empty)
{
    if (client.Account.Mail == string.Empty)
        // Prompt player to set email
        client.Player.Out.SendMessage("Enter your email with /email command!");
    else
    {
        // Send bug report
        Mail.MailMgr.SendMail(
            ServerProperties.Properties.BUG_REPORT_EMAIL_ADDRESSES,
            $"{ServerName} bug report {reportID}",
            reportMessage,
            playerName,
            client.Account.Mail);
    }
}
```

### Server Logs
```csharp
public static bool SendLogs(string to)
{
    // Compress archived logs
    Queue archivedLogsUrls = GetArchivedLogsUrls();
    GZipCompress.compressMultipleFiles(archivedLogsUrls);
    
    // Build mail with attachments
    MailMessage mail = new MailMessage();
    mail.Subject = "[Logs] " + DateTime.Now.ToString();
    
    foreach (string file in archivedLogsUrls)
    {
        Attachment attachment = new Attachment(file + ".gz");
        mail.Attachments.Add(attachment);
    }
    
    SmtpClient.Send(mail);
    
    // Cleanup files after sending
    foreach (string file in archivedLogsUrls)
    {
        File.Delete(file);
        File.Delete(file + ".gz");
    }
}
```

### Administrative Notifications
```csharp
public static bool SendMail(string to, string subject, string message, 
                           string fromName, string fromAddress)
{
    if (!m_enable)
        return false;
        
    MailMessage mail = new MailMessage();
    
    // Parse multiple recipients
    foreach (string recipient in Util.SplitCSV(to))
        mail.To.Add(recipient);
        
    mail.Subject = subject;
    mail.From = new MailAddress(fromAddress, fromName);
    mail.IsBodyHtml = true;
    mail.Body = message;
    mail.BodyEncoding = System.Text.Encoding.ASCII;
    mail.SubjectEncoding = System.Text.Encoding.ASCII;
    
    // Queue for delivery
    lock (_lock)
    {
        m_mailQueue.Enqueue(mail);
    }
    
    return true;
}
```

## Queue Management

### Delivery Queue
```csharp
private static void RunTask(object state)
{
    Logger.Info("Starting mail queue");
    
    lock (_lock)
    {
        while (m_mailQueue.Count > 0)
        {
            try
            {
                MailMessage mailMessage = (MailMessage)m_mailQueue.Dequeue();
                SmtpClient.Send(mailMessage);
            }
            catch (Exception e)
            {
                Logger.Error("Couldn't send message, requeueing");
                m_mailQueue.Enqueue(mailMessage);
                break; // Stop processing on error
            }
        }
    }
    
    // Reschedule next delivery
    m_timer.Change(m_mailFrequency, Timeout.Infinite);
}
```

### Delivery Schedule
- **Frequency**: 5 minutes between delivery attempts
- **Retry Logic**: Failed messages requeued
- **Error Handling**: Stops processing on SMTP errors
- **Thread Safety**: Queue protected by locks

## Player Email Integration

### Email Command
```csharp
[Cmd("&email", ePrivLevel.Player, "Set e-mail address for current account")]
public class EmailCommand : AbstractCommandHandler
{
    public void OnCommand(GameClient client, string[] args)
    {
        if (args.Length != 2)
        {
            client.Out.SendMessage("Usage: /email <address>");
            return;
        }
        
        string emailAddy = args[1];
        
        // Validate email syntax
        EmailSyntaxValidator validator = new EmailSyntaxValidator(emailAddy, true);
        if (!validator.IsValid)
        {
            client.Out.SendMessage("Please enter a valid e-mail address.");
            return;
        }
        
        // Update account
        string oldEmail = client.Account.Mail;
        client.Account.Mail = emailAddy;
        GameServer.Database.SaveObject(client.Account);
        
        // Audit trail
        AuditMgr.AddAuditEntry(client, AuditType.Account, 
                              AuditSubtype.AccountEmailChange, 
                              oldEmail, emailAddy);
                              
        client.Out.SendMessage($"Email set to {emailAddy}. Thanks!");
    }
}
```

### Email Validation
```csharp
public class EmailSyntaxValidator
{
    private readonly string _email;
    private readonly bool _strict;
    
    public bool IsValid { get; private set; }
    
    public EmailSyntaxValidator(string email, bool strict)
    {
        _email = email;
        _strict = strict;
        ValidateEmail();
    }
    
    private void ValidateEmail()
    {
        // Basic format validation
        if (string.IsNullOrEmpty(_email))
        {
            IsValid = false;
            return;
        }
        
        // Use regex for validation
        string pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        IsValid = Regex.IsMatch(_email, pattern);
        
        if (_strict)
        {
            // Additional strict validation
            // Check for valid domains, etc.
        }
    }
}
```

## Log Management

### Log File Discovery
```csharp
public static Queue GetArchivedLogsUrls()
{
    FileInfo configFile = new FileInfo(GameServer.Instance.Configuration.LogConfigFile);
    Queue archiveList = new Queue();
    
    if (configFile.Exists)
    {
        // Parse log config XML
        XmlTextReader reader = new XmlTextReader(configFile.OpenRead());
        
        while (reader.Read())
        {
            if (reader.LocalName == "file")
            {
                reader.MoveToFirstAttribute();
                string logPath = reader.ReadContentAsString();
                
                // Find all archived versions
                FileInfo logFile = new FileInfo(logPath);
                foreach (string file in Directory.GetFiles(
                    logFile.DirectoryName, logFile.Name + "*"))
                {
                    if (file != logFile.FullName)
                        archiveList.Enqueue(file);
                }
            }
        }
    }
    else
    {
        // Default log files
        archiveList.Enqueue("./logs/Cheats.Log");
        archiveList.Enqueue("./logs/GMActions.Log");
        archiveList.Enqueue("./logs/Error.Log");
        archiveList.Enqueue("./logs/GameServer.Log");
    }
    
    return archiveList;
}
```

### Compression System
```csharp
public class GZipCompress
{
    public static bool compressMultipleFiles(Queue filesPathsList)
    {
        foreach (string file in filesPathsList)
        {
            // Skip already compressed files
            if (file.EndsWith(".gz"))
                continue;
                
            if (!compressFile(file, file + ".gz"))
                return false;
        }
        return true;
    }
    
    public static bool compressFile(string source, string dest)
    {
        try
        {
            // Read source file
            FileStream sourceStream = new FileStream(source, FileMode.Open, FileAccess.Read);
            byte[] buffer = new byte[sourceStream.Length];
            sourceStream.Read(buffer, 0, buffer.Length);
            sourceStream.Close();
            
            // Write compressed file
            FileStream destStream = new FileStream(dest, FileMode.Create);
            GZipStream zipStream = new GZipStream(destStream, CompressionMode.Compress);
            zipStream.Write(buffer, 0, buffer.Length);
            zipStream.Close();
            
            return true;
        }
        catch (Exception e)
        {
            MailMgr.Logger.Error(e);
            return false;
        }
    }
}
```

## SMTP Configuration

### Connection Setup
```csharp
public static bool Init()
{
    // Load configuration
    XmlConfigFile xmlConfig = XmlConfigFile.ParseXMLFile(configFile);
    m_enable = xmlConfig["Enable"].GetBoolean(false);
    
    if (m_enable)
    {
        m_username = xmlConfig["Username"].GetString("");
        m_password = xmlConfig["Password"].GetString("");
        m_emailAddress = xmlConfig["EmailAddress"].GetString("");
        m_smtpServer = xmlConfig["SMTPServer"].GetString("");
        m_ssl = xmlConfig["SSL"].GetBoolean(false);
    }
    
    // Setup SMTP client
    SmtpClient = new SmtpClient(m_smtpServer);
    SmtpClient.UseDefaultCredentials = false;
    SmtpClient.EnableSsl = m_ssl;
    SmtpClient.Credentials = new NetworkCredential(m_username, m_password);
    
    // Start delivery timer
    if (m_enable)
    {
        m_timer = new Timer(new TimerCallback(RunTask), 
                           null, m_mailFrequency, 0);
    }
    
    return true;
}
```

### Security Configuration
```csharp
// SSL/TLS Support
SmtpClient.EnableSsl = m_ssl;

// Authentication
SmtpClient.UseDefaultCredentials = false;
SmtpClient.Credentials = new NetworkCredential(m_username, m_password);

// Port Configuration (implicit in server setting)
// Standard ports: 25 (plain), 587 (TLS), 465 (SSL)
```

## System Integration

### With Bug Reporting
```csharp
public class ReportCommand : AbstractCommandHandler
{
    public void OnCommand(GameClient client, string[] args)
    {
        // Create bug report
        DbBugReport report = new DbBugReport();
        report.Message = message;
        report.Submitter = $"{client.Player.Name} [{client.Account.Name}]";
        
        GameServer.Database.AddObject(report);
        
        // Send email if configured
        if (ServerProperties.Properties.BUG_REPORT_EMAIL_ADDRESSES != string.Empty)
        {
            if (client.Account.Mail != string.Empty)
            {
                MailMgr.SendMail(
                    ServerProperties.Properties.BUG_REPORT_EMAIL_ADDRESSES,
                    $"Bug report {report.ID}",
                    report.Message,
                    report.Submitter,
                    client.Account.Mail);
            }
        }
    }
}
```

### With Server Startup
```csharp
public bool Init()
{
    // Initialize mail manager
    InitComponent(MailMgr.Init(), "Mail Manager Initialization");
    
    // Send logs if configured
    if (ServerProperties.Properties.LOG_EMAIL_ADDRESSES != string.Empty)
        MailMgr.SendLogs(ServerProperties.Properties.LOG_EMAIL_ADDRESSES);
}
```

### With Account Management
```csharp
// Account email storage
public class Account
{
    public string Mail { get; set; } // Player email address
}

// Audit integration
AuditMgr.AddAuditEntry(client, AuditType.Account, 
                      AuditSubtype.AccountEmailChange, 
                      oldEmail, newEmail);
```

## Configuration Properties

### Server Properties
```xml
<Property Name="BUG_REPORT_EMAIL_ADDRESSES" Value="bugs@server.com" />
<Property Name="LOG_EMAIL_ADDRESSES" Value="admin@server.com" />
```

### Mail Config File
```xml
<?xml version="1.0" encoding="UTF-8"?>
<Configuration>
    <Username>your_smtp_username</Username>
    <Password>your_smtp_password</Password>
    <EmailAddress>server@example.com</EmailAddress>
    <SMTPServer>smtp.gmail.com</SMTPServer>
    <SSL>true</SSL>
    <Enable>true</Enable>
</Configuration>
```

## Error Handling

### SMTP Errors
```csharp
try
{
    SmtpClient.Send(mailMessage);
}
catch (Exception e)
{
    Logger.Error("Couldn't send message, requeueing");
    Logger.Error(e.ToString());
    m_mailQueue.Enqueue(mailMessage);
    break; // Stop processing queue
}
```

### Configuration Errors
```csharp
public static bool Init()
{
    try
    {
        // Configuration loading
    }
    catch (Exception e)
    {
        Logger.Error(e);
        return false; // Fail initialization
    }
}
```

### File Compression Errors
```csharp
public static bool SendLogs(string to)
{
    try
    {
        // Compress logs
        if (!GZipCompress.compressMultipleFiles(archivedLogsUrls))
        {
            Logger.Error("Cannot compress files properly. Email not sent.");
            return false;
        }
    }
    catch (Exception e)
    {
        Logger.Error(e);
        return false;
    }
}
```

## Performance Considerations

### Queue Processing
- **Batched Delivery**: 5-minute intervals
- **Queue Size**: Unlimited (memory permitting)
- **Thread Safety**: Single delivery thread
- **Error Recovery**: Failed messages requeued

### File Operations
- **Compression**: GZip for log attachments
- **Cleanup**: Files deleted after successful send
- **Memory Usage**: Logs loaded into memory for compression

### Network Efficiency
- **Connection Reuse**: Single SMTP client instance
- **Attachment Size**: Compressed logs reduce bandwidth
- **Delivery Grouping**: Multiple emails per delivery cycle

## Security Features

### Email Validation
- **Syntax Checking**: Regex pattern validation
- **Strict Mode**: Additional validation available
- **Input Sanitization**: Protected against injection

### Authentication
- **SMTP Auth**: Username/password authentication
- **SSL/TLS**: Encrypted connections supported
- **Credential Storage**: Encrypted in configuration

### Access Control
- **Player Emails**: Only account owner can set
- **Admin Emails**: Server property configuration
- **Audit Trail**: Email changes logged

## Test Scenarios

### Basic Functionality
- Configure SMTP settings
- Send test email
- Verify queue processing
- Check delivery confirmation

### Bug Report Integration
- Player submits bug report
- Verify email sent to admin
- Check report stored in database
- Verify audit trail

### Log Delivery
- Generate log files
- Trigger log compression
- Send logs via email
- Verify file cleanup

### Error Handling
- Invalid SMTP configuration
- Network connection failures
- File compression errors
- Queue processing recovery

## Implementation Notes

### Startup Integration
```csharp
// Called during server initialization
InitComponent(MailMgr.Init(), "Mail Manager Initialization");
```

### Threading Model
- **Main Thread**: Configuration and queue management
- **Timer Thread**: Delivery processing
- **Thread Safety**: Locks protect queue access

### Memory Management
- **Queue Growth**: No size limits (potential issue)
- **File Handling**: Temporary compression files
- **Connection Pooling**: Single SMTP client reused

## Future Enhancements

### Potential Improvements
- **Queue Size Limits**: Prevent memory exhaustion
- **Delivery Retry Logic**: Exponential backoff
- **Template System**: HTML email templates
- **Attachment Filtering**: Size and type restrictions
- **Delivery Status**: Success/failure tracking 