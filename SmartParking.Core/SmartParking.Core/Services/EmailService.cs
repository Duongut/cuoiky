using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using SmartParking.Core.Models;
using SmartParking.Core.Utils;
using System;
using System.Threading.Tasks;

namespace SmartParking.Core.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string recipientEmail, string recipientName, string subject, string htmlBody)
        {
            try
            {
                var emailSettings = _configuration.GetSection("EmailSettings");
                var smtpServer = emailSettings["SmtpServer"];
                var smtpPort = int.Parse(emailSettings["SmtpPort"]);
                var smtpUsername = emailSettings["SmtpUsername"];
                var smtpPassword = emailSettings["SmtpPassword"];
                var senderEmail = emailSettings["SenderEmail"];
                var senderName = emailSettings["SenderName"];

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(senderName, senderEmail));
                message.To.Add(new MailboxAddress(recipientName, recipientEmail));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = htmlBody
                };

                message.Body = bodyBuilder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(smtpUsername, smtpPassword);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }

                _logger.LogInformation($"Email sent to {recipientEmail} with subject: {subject}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending email to {recipientEmail}");
                throw;
            }
        }

        public async Task SendRegistrationConfirmationAsync(MonthlyVehicle vehicle)
        {
            var subject = $"Monthly Parking Registration Confirmation - {vehicle.LicensePlate}";
            
            var htmlBody = $@"
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background-color: #4CAF50; color: white; padding: 10px; text-align: center; }}
                    .content {{ padding: 20px; border: 1px solid #ddd; }}
                    .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #777; }}
                    table {{ width: 100%; border-collapse: collapse; margin: 20px 0; }}
                    th, td {{ padding: 12px; text-align: left; border-bottom: 1px solid #ddd; }}
                    th {{ background-color: #f2f2f2; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>Monthly Parking Registration Confirmation</h1>
                    </div>
                    <div class='content'>
                        <p>Dear {vehicle.CustomerName},</p>
                        <p>Thank you for registering your vehicle for monthly parking. Your registration has been confirmed.</p>
                        
                        <h2>Registration Details:</h2>
                        <table>
                            <tr>
                                <th>Vehicle ID</th>
                                <td>{vehicle.VehicleId}</td>
                            </tr>
                            <tr>
                                <th>License Plate</th>
                                <td>{vehicle.LicensePlate}</td>
                            </tr>
                            <tr>
                                <th>Vehicle Type</th>
                                <td>{vehicle.VehicleType}</td>
                            </tr>
                            <tr>
                                <th>Fixed Parking Slot</th>
                                <td>{vehicle.FixedSlotId}</td>
                            </tr>
                            <tr>
                                <th>Start Date</th>
                                <td>{vehicle.StartDate.FormatVietnamDateTime()}</td>
                            </tr>
                            <tr>
                                <th>End Date</th>
                                <td>{vehicle.EndDate.FormatVietnamDateTime()}</td>
                            </tr>
                            <tr>
                                <th>Package Duration</th>
                                <td>{vehicle.PackageDuration} month(s)</td>
                            </tr>
                            <tr>
                                <th>Package Amount</th>
                                <td>{vehicle.PackageAmount:N0} VND</td>
                            </tr>
                        </table>
                        
                        <p>Your monthly parking package is now active. You can use your assigned parking slot at any time.</p>
                        <p>We will send you a reminder before your package expires.</p>
                        
                        <p>Thank you for choosing our parking service!</p>
                        
                        <p>Best regards,<br>Smart Parking System</p>
                    </div>
                    <div class='footer'>
                        <p>This is an automated email. Please do not reply to this message.</p>
                        <p>&copy; {DateTime.Now.Year} Smart Parking System. All rights reserved.</p>
                    </div>
                </div>
            </body>
            </html>";

            await SendEmailAsync(vehicle.CustomerEmail, vehicle.CustomerName, subject, htmlBody);
        }

        public async Task SendRenewalConfirmationAsync(MonthlyVehicle vehicle)
        {
            var subject = $"Monthly Parking Renewal Confirmation - {vehicle.LicensePlate}";
            
            var htmlBody = $@"
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background-color: #2196F3; color: white; padding: 10px; text-align: center; }}
                    .content {{ padding: 20px; border: 1px solid #ddd; }}
                    .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #777; }}
                    table {{ width: 100%; border-collapse: collapse; margin: 20px 0; }}
                    th, td {{ padding: 12px; text-align: left; border-bottom: 1px solid #ddd; }}
                    th {{ background-color: #f2f2f2; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>Monthly Parking Renewal Confirmation</h1>
                    </div>
                    <div class='content'>
                        <p>Dear {vehicle.CustomerName},</p>
                        <p>Thank you for renewing your monthly parking package. Your renewal has been confirmed.</p>
                        
                        <h2>Renewal Details:</h2>
                        <table>
                            <tr>
                                <th>Vehicle ID</th>
                                <td>{vehicle.VehicleId}</td>
                            </tr>
                            <tr>
                                <th>License Plate</th>
                                <td>{vehicle.LicensePlate}</td>
                            </tr>
                            <tr>
                                <th>Vehicle Type</th>
                                <td>{vehicle.VehicleType}</td>
                            </tr>
                            <tr>
                                <th>Fixed Parking Slot</th>
                                <td>{vehicle.FixedSlotId}</td>
                            </tr>
                            <tr>
                                <th>New End Date</th>
                                <td>{vehicle.EndDate.FormatVietnamDateTime()}</td>
                            </tr>
                            <tr>
                                <th>Package Duration</th>
                                <td>{vehicle.PackageDuration} month(s)</td>
                            </tr>
                            <tr>
                                <th>Package Amount</th>
                                <td>{vehicle.PackageAmount:N0} VND</td>
                            </tr>
                            <tr>
                                <th>Renewal Date</th>
                                <td>{vehicle.LastRenewalDate?.FormatVietnamDateTime()}</td>
                            </tr>
                        </table>
                        
                        <p>Your monthly parking package has been renewed. You can continue to use your assigned parking slot at any time.</p>
                        <p>We will send you a reminder before your package expires.</p>
                        
                        <p>Thank you for your continued trust in our parking service!</p>
                        
                        <p>Best regards,<br>Smart Parking System</p>
                    </div>
                    <div class='footer'>
                        <p>This is an automated email. Please do not reply to this message.</p>
                        <p>&copy; {DateTime.Now.Year} Smart Parking System. All rights reserved.</p>
                    </div>
                </div>
            </body>
            </html>";

            await SendEmailAsync(vehicle.CustomerEmail, vehicle.CustomerName, subject, htmlBody);
        }

        public async Task SendExpirationReminderAsync(MonthlyVehicle vehicle)
        {
            var daysRemaining = (vehicle.EndDate - DateTimeUtils.GetVietnamNow()).Days;
            var subject = $"Monthly Parking Package Expiration Reminder - {vehicle.LicensePlate}";
            
            var htmlBody = $@"
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background-color: #FF9800; color: white; padding: 10px; text-align: center; }}
                    .content {{ padding: 20px; border: 1px solid #ddd; }}
                    .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #777; }}
                    .button {{ display: inline-block; background-color: #4CAF50; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; }}
                    table {{ width: 100%; border-collapse: collapse; margin: 20px 0; }}
                    th, td {{ padding: 12px; text-align: left; border-bottom: 1px solid #ddd; }}
                    th {{ background-color: #f2f2f2; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>Monthly Parking Package Expiration Reminder</h1>
                    </div>
                    <div class='content'>
                        <p>Dear {vehicle.CustomerName},</p>
                        <p>This is a reminder that your monthly parking package will expire in <strong>{daysRemaining} day(s)</strong>.</p>
                        
                        <h2>Package Details:</h2>
                        <table>
                            <tr>
                                <th>Vehicle ID</th>
                                <td>{vehicle.VehicleId}</td>
                            </tr>
                            <tr>
                                <th>License Plate</th>
                                <td>{vehicle.LicensePlate}</td>
                            </tr>
                            <tr>
                                <th>Vehicle Type</th>
                                <td>{vehicle.VehicleType}</td>
                            </tr>
                            <tr>
                                <th>Fixed Parking Slot</th>
                                <td>{vehicle.FixedSlotId}</td>
                            </tr>
                            <tr>
                                <th>Expiration Date</th>
                                <td>{vehicle.EndDate.FormatVietnamDateTime()}</td>
                            </tr>
                        </table>
                        
                        <p>To continue enjoying the benefits of monthly parking, please renew your package before it expires.</p>
                        <p>If you do not renew your package, your vehicle will be treated as a casual vehicle and charged accordingly when leaving the parking lot.</p>
                        
                        <p>Thank you for choosing our parking service!</p>
                        
                        <p>Best regards,<br>Smart Parking System</p>
                    </div>
                    <div class='footer'>
                        <p>This is an automated email. Please do not reply to this message.</p>
                        <p>&copy; {DateTime.Now.Year} Smart Parking System. All rights reserved.</p>
                    </div>
                </div>
            </body>
            </html>";

            await SendEmailAsync(vehicle.CustomerEmail, vehicle.CustomerName, subject, htmlBody);
        }

        public async Task SendExpirationNotificationAsync(MonthlyVehicle vehicle)
        {
            var subject = $"Monthly Parking Package Expired - {vehicle.LicensePlate}";
            
            var htmlBody = $@"
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background-color: #F44336; color: white; padding: 10px; text-align: center; }}
                    .content {{ padding: 20px; border: 1px solid #ddd; }}
                    .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #777; }}
                    .button {{ display: inline-block; background-color: #4CAF50; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; }}
                    table {{ width: 100%; border-collapse: collapse; margin: 20px 0; }}
                    th, td {{ padding: 12px; text-align: left; border-bottom: 1px solid #ddd; }}
                    th {{ background-color: #f2f2f2; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>Monthly Parking Package Expired</h1>
                    </div>
                    <div class='content'>
                        <p>Dear {vehicle.CustomerName},</p>
                        <p>Your monthly parking package has expired.</p>
                        
                        <h2>Package Details:</h2>
                        <table>
                            <tr>
                                <th>Vehicle ID</th>
                                <td>{vehicle.VehicleId}</td>
                            </tr>
                            <tr>
                                <th>License Plate</th>
                                <td>{vehicle.LicensePlate}</td>
                            </tr>
                            <tr>
                                <th>Vehicle Type</th>
                                <td>{vehicle.VehicleType}</td>
                            </tr>
                            <tr>
                                <th>Expiration Date</th>
                                <td>{vehicle.EndDate.FormatVietnamDateTime()}</td>
                            </tr>
                        </table>
                        
                        <p>Your fixed parking slot has been released and is no longer reserved for your vehicle.</p>
                        <p>Your vehicle will now be treated as a casual vehicle and charged accordingly when leaving the parking lot.</p>
                        
                        <p>To continue enjoying the benefits of monthly parking, please renew your package as soon as possible.</p>
                        
                        <p>Thank you for choosing our parking service!</p>
                        
                        <p>Best regards,<br>Smart Parking System</p>
                    </div>
                    <div class='footer'>
                        <p>This is an automated email. Please do not reply to this message.</p>
                        <p>&copy; {DateTime.Now.Year} Smart Parking System. All rights reserved.</p>
                    </div>
                </div>
            </body>
            </html>";

            await SendEmailAsync(vehicle.CustomerEmail, vehicle.CustomerName, subject, htmlBody);
        }

        public async Task SendCancellationNotificationAsync(MonthlyVehicle vehicle)
        {
            var subject = $"Monthly Parking Package Cancelled - {vehicle.LicensePlate}";
            
            var htmlBody = $@"
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background-color: #9C27B0; color: white; padding: 10px; text-align: center; }}
                    .content {{ padding: 20px; border: 1px solid #ddd; }}
                    .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #777; }}
                    table {{ width: 100%; border-collapse: collapse; margin: 20px 0; }}
                    th, td {{ padding: 12px; text-align: left; border-bottom: 1px solid #ddd; }}
                    th {{ background-color: #f2f2f2; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>Monthly Parking Package Cancelled</h1>
                    </div>
                    <div class='content'>
                        <p>Dear {vehicle.CustomerName},</p>
                        <p>Your monthly parking package has been cancelled as requested.</p>
                        
                        <h2>Package Details:</h2>
                        <table>
                            <tr>
                                <th>Vehicle ID</th>
                                <td>{vehicle.VehicleId}</td>
                            </tr>
                            <tr>
                                <th>License Plate</th>
                                <td>{vehicle.LicensePlate}</td>
                            </tr>
                            <tr>
                                <th>Vehicle Type</th>
                                <td>{vehicle.VehicleType}</td>
                            </tr>
                            <tr>
                                <th>Cancellation Date</th>
                                <td>{DateTimeUtils.GetVietnamNow().FormatVietnamDateTime()}</td>
                            </tr>
                        </table>
                        
                        <p>Your fixed parking slot has been released and is no longer reserved for your vehicle.</p>
                        <p>Your vehicle will now be treated as a casual vehicle and charged accordingly when leaving the parking lot.</p>
                        
                        <p>If you wish to register for monthly parking again in the future, please contact our customer service.</p>
                        
                        <p>Thank you for choosing our parking service!</p>
                        
                        <p>Best regards,<br>Smart Parking System</p>
                    </div>
                    <div class='footer'>
                        <p>This is an automated email. Please do not reply to this message.</p>
                        <p>&copy; {DateTime.Now.Year} Smart Parking System. All rights reserved.</p>
                    </div>
                </div>
            </body>
            </html>";

            await SendEmailAsync(vehicle.CustomerEmail, vehicle.CustomerName, subject, htmlBody);
        }
    }
}
