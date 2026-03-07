import { useState } from "react";
import {
  Bell,
  Moon,
  Shield,
  CreditCard,
  Download,
  Trash2,
  Mail,
  Smartphone,
  Monitor,
  Save,
} from "lucide-react";
import { Button } from "./ui/button";
import { Switch } from "./ui/switch";
import { Label } from "./ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "./ui/select";
import { Separator } from "./ui/separator";

export function Settings() {
  const [settings, setSettings] = useState({
    // Notifications
    emailNotifications: true,
    pushNotifications: false,
    smsNotifications: true,
    transactionAlerts: true,
    securityAlerts: true,
    marketingEmails: false,

    // Appearance
    theme: "dark",
    language: "en",

    // Privacy
    publicProfile: false,
    showActivity: true,
    dataCollection: false,

    // Security
    twoFactorAuth: false,
    biometric: true,
    sessionTimeout: "30",
  });

  const [isSaving, setIsSaving] = useState(false);

  const handleToggle = (key: string) => {
    setSettings((prev) => ({ ...prev, [key]: !prev[key as keyof typeof prev] }));
  };

  const handleSelectChange = (key: string, value: string) => {
    setSettings((prev) => ({ ...prev, [key]: value }));
  };

  const handleSave = () => {
    setIsSaving(true);
    setTimeout(() => {
      setIsSaving(false);
    }, 1000);
  };

  return (
    <div className="max-w-4xl mx-auto space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl text-foreground">Settings</h1>
          <p className="text-muted-foreground mt-1">
            Manage your account preferences
          </p>
        </div>

        <Button onClick={handleSave} disabled={isSaving}>
          <Save className="w-4 h-4 mr-2" />
          {isSaving ? "Saving..." : "Save"}
        </Button>
      </div>

      {/* Notifications Section */}
      <div className="bg-card border border-border rounded-lg p-6 space-y-6">
        <div className="flex items-center gap-3">
          <Bell className="w-5 h-5 text-accent" />
          <h2 className="text-xl text-foreground">Notifications</h2>
        </div>

        <Separator />

        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <div className="space-y-0.5">
              <Label htmlFor="emailNotifications">Email notifications</Label>
              <p className="text-sm text-muted-foreground">
                Receive notifications by email
              </p>
            </div>
            <Switch
              id="emailNotifications"
              checked={settings.emailNotifications}
              onCheckedChange={() => handleToggle("emailNotifications")}
            />
          </div>

          <div className="flex items-center justify-between">
            <div className="space-y-0.5">
              <Label htmlFor="pushNotifications">Push notifications</Label>
              <p className="text-sm text-muted-foreground">
                Receive browser notifications
              </p>
            </div>
            <Switch
              id="pushNotifications"
              checked={settings.pushNotifications}
              onCheckedChange={() => handleToggle("pushNotifications")}
            />
          </div>

          <div className="flex items-center justify-between">
            <div className="space-y-0.5">
              <Label htmlFor="smsNotifications">SMS notifications</Label>
              <p className="text-sm text-muted-foreground">
                Receive notifications by SMS
              </p>
            </div>
            <Switch
              id="smsNotifications"
              checked={settings.smsNotifications}
              onCheckedChange={() => handleToggle("smsNotifications")}
            />
          </div>

          <Separator />

          <div className="flex items-center justify-between">
            <div className="space-y-0.5">
              <Label htmlFor="transactionAlerts">Transaction alerts</Label>
              <p className="text-sm text-muted-foreground">
                Notifications for every transaction
              </p>
            </div>
            <Switch
              id="transactionAlerts"
              checked={settings.transactionAlerts}
              onCheckedChange={() => handleToggle("transactionAlerts")}
            />
          </div>

          <div className="flex items-center justify-between">
            <div className="space-y-0.5">
              <Label htmlFor="securityAlerts">Security alerts</Label>
              <p className="text-sm text-muted-foreground">
                Critical security notifications
              </p>
            </div>
            <Switch
              id="securityAlerts"
              checked={settings.securityAlerts}
              onCheckedChange={() => handleToggle("securityAlerts")}
            />
          </div>

          <div className="flex items-center justify-between">
            <div className="space-y-0.5">
              <Label htmlFor="marketingEmails">Marketing emails</Label>
              <p className="text-sm text-muted-foreground">
                News and offers
              </p>
            </div>
            <Switch
              id="marketingEmails"
              checked={settings.marketingEmails}
              onCheckedChange={() => handleToggle("marketingEmails")}
            />
          </div>
        </div>
      </div>

      {/* Appearance Section */}
      <div className="bg-card border border-border rounded-lg p-6 space-y-6">
        <div className="flex items-center gap-3">
          <Moon className="w-5 h-5 text-accent" />
          <h2 className="text-xl text-foreground">Appearance</h2>
        </div>

        <Separator />

        <div className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="theme">Theme</Label>
            <Select
              value={settings.theme}
              onValueChange={(value) => handleSelectChange("theme", value)}
            >
              <SelectTrigger id="theme" className="bg-input-background">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="light">Light</SelectItem>
                <SelectItem value="dark">Dark</SelectItem>
                <SelectItem value="auto">System</SelectItem>
              </SelectContent>
            </Select>
          </div>

          <div className="space-y-2">
            <Label htmlFor="language">Language</Label>
            <Select
              value={settings.language}
              onValueChange={(value) => handleSelectChange("language", value)}
            >
              <SelectTrigger id="language" className="bg-input-background">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="en">English</SelectItem>
                <SelectItem value="es">Español</SelectItem>
                <SelectItem value="zh">中文</SelectItem>
              </SelectContent>
            </Select>
          </div>
        </div>
      </div>

      {/* Privacy Section */}
      <div className="bg-card border border-border rounded-lg p-6 space-y-6">
        <div className="flex items-center gap-3">
          <Shield className="w-5 h-5 text-accent" />
          <h2 className="text-xl text-foreground">Privacy</h2>
        </div>

        <Separator />

        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <div className="space-y-0.5">
              <Label htmlFor="publicProfile">Public profile</Label>
              <p className="text-sm text-muted-foreground">
                Make profile visible to others
              </p>
            </div>
            <Switch
              id="publicProfile"
              checked={settings.publicProfile}
              onCheckedChange={() => handleToggle("publicProfile")}
            />
          </div>

          <div className="flex items-center justify-between">
            <div className="space-y-0.5">
              <Label htmlFor="showActivity">Show activity</Label>
              <p className="text-sm text-muted-foreground">
                Display your activity
              </p>
            </div>
            <Switch
              id="showActivity"
              checked={settings.showActivity}
              onCheckedChange={() => handleToggle("showActivity")}
            />
          </div>

          <div className="flex items-center justify-between">
            <div className="space-y-0.5">
              <Label htmlFor="dataCollection">Data collection for analytics</Label>
              <p className="text-sm text-muted-foreground">
                Helps improve the service
              </p>
            </div>
            <Switch
              id="dataCollection"
              checked={settings.dataCollection}
              onCheckedChange={() => handleToggle("dataCollection")}
            />
          </div>
        </div>
      </div>

      {/* Security Section */}
      <div className="bg-card border border-border rounded-lg p-6 space-y-6">
        <div className="flex items-center gap-3">
          <Shield className="w-5 h-5 text-accent" />
          <h2 className="text-xl text-foreground">Security</h2>
        </div>

        <Separator />

        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <div className="space-y-0.5">
              <Label htmlFor="twoFactorAuth">Two-factor authentication</Label>
              <p className="text-sm text-muted-foreground">
                Extra layer of protection
              </p>
            </div>
            <Switch
              id="twoFactorAuth"
              checked={settings.twoFactorAuth}
              onCheckedChange={() => handleToggle("twoFactorAuth")}
            />
          </div>

          <div className="flex items-center justify-between">
            <div className="space-y-0.5">
              <Label htmlFor="biometric">Biometric authentication</Label>
              <p className="text-sm text-muted-foreground">
                Sign in with fingerprint or Face ID
              </p>
            </div>
            <Switch
              id="biometric"
              checked={settings.biometric}
              onCheckedChange={() => handleToggle("biometric")}
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="sessionTimeout">Session timeout</Label>
            <Select
              value={settings.sessionTimeout}
              onValueChange={(value) => handleSelectChange("sessionTimeout", value)}
            >
              <SelectTrigger id="sessionTimeout" className="bg-input-background">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="15">15 minutes</SelectItem>
                <SelectItem value="30">30 minutes</SelectItem>
                <SelectItem value="60">1 hour</SelectItem>
                <SelectItem value="never">Never</SelectItem>
              </SelectContent>
            </Select>
          </div>
        </div>
      </div>

      {/* Connected Devices */}
      <div className="bg-card border border-border rounded-lg p-6 space-y-6">
        <div className="flex items-center gap-3">
          <Monitor className="w-5 h-5 text-accent" />
          <h2 className="text-xl text-foreground">Connected devices</h2>
        </div>

        <Separator />

        <div className="space-y-3">
          {[
            {
              icon: Monitor,
              name: "Chrome on Windows",
              location: "New York, USA",
              lastActive: "Active now",
              current: true,
            },
            {
              icon: Smartphone,
              name: "iPhone 14 Pro",
              location: "New York, USA",
              lastActive: "2 hours ago",
              current: false,
            },
            {
              icon: Monitor,
              name: "Safari on MacBook",
              location: "San Francisco, USA",
              lastActive: "1 day ago",
              current: false,
            },
          ].map((device, index) => {
            const Icon = device.icon;
            return (
              <div
                key={index}
                className="flex items-center justify-between p-4 bg-secondary/30 rounded-lg"
              >
                <div className="flex items-center gap-3">
                  <Icon className="w-5 h-5 text-muted-foreground" />
                  <div>
                    <div className="flex items-center gap-2">
                      <p className="text-foreground">{device.name}</p>
                      {device.current && (
                        <span className="text-xs px-2 py-0.5 rounded bg-success/10 text-success border border-success/30">
                          Current
                        </span>
                      )}
                    </div>
                    <p className="text-sm text-muted-foreground">
                      {device.location} • {device.lastActive}
                    </p>
                  </div>
                </div>
                {!device.current && (
                  <Button variant="ghost" size="sm">
                    Disconnect
                  </Button>
                )}
              </div>
            );
          })}
        </div>
      </div>

      {/* Data & Privacy */}
      <div className="bg-card border border-border rounded-lg p-6 space-y-6">
        <div className="flex items-center gap-3">
          <Download className="w-5 h-5 text-accent" />
          <h2 className="text-xl text-foreground">Data & privacy</h2>
        </div>

        <Separator />

        <div className="space-y-3">
          <Button variant="outline" className="w-full justify-start">
            <Download className="w-4 h-4 mr-2" />
            Download my data
          </Button>

          <Button variant="outline" className="w-full justify-start">
            <CreditCard className="w-4 h-4 mr-2" />
            Export transaction history
          </Button>

          <Button
            variant="outline"
            className="w-full justify-start text-destructive hover:text-destructive hover:bg-destructive/10"
          >
            <Trash2 className="w-4 h-4 mr-2" />
            Delete account
          </Button>
        </div>
      </div>

      {/* Support */}
      <div className="bg-accent/10 border border-accent/30 rounded-lg p-4">
        <div className="flex items-start gap-3">
          <Mail className="w-5 h-5 text-accent flex-shrink-0 mt-0.5" />
          <div>
            <p className="text-sm text-accent-foreground/90">
              <strong>Need help?</strong>
            </p>
            <p className="text-sm text-muted-foreground mt-1">
              Contact our support team at{" "}
              <a
                href="mailto:support@financehub.com"
                className="text-accent hover:text-accent/80"
              >
                support@financehub.com
              </a>
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}
