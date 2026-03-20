import { useEffect, useMemo, useState } from "react";
import {
  Bell,
  Moon,
  Shield,
  Download,
  Trash2,
  Mail,
  Smartphone,
  Monitor,
  Save,
  AlertCircle,
} from "lucide-react";
import { Button } from "./ui/button";
import { Switch } from "./ui/switch";
import { Label } from "./ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "./ui/select";
import { Separator } from "./ui/separator";
import {
  getMyProfile,
  getMySettings,
  updateMySettings,
  updateUserSettingsPolicy,
  type ApiUserSettings,
} from "../api/client";

const POLICY_FIELDS = [
  "emailNotifications",
  "pushNotifications",
  "smsNotifications",
  "transactionAlerts",
  "securityAlerts",
  "marketingEmails",
  "theme",
  "language",
  "publicProfile",
  "showActivity",
  "dataCollection",
  "twoFactorAuth",
  "biometric",
  "sessionTimeout",
] as const;

export function Settings() {
  const [settings, setSettings] = useState<ApiUserSettings | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [isAdmin, setIsAdmin] = useState(false);
  const [policyTargetUid, setPolicyTargetUid] = useState("");
  const [policyLockedFields, setPolicyLockedFields] = useState<string[]>([]);
  const [isUpdatingPolicy, setIsUpdatingPolicy] = useState(false);

  useEffect(() => {
    async function loadData() {
      try {
        setError(null);
        setIsLoading(true);

        const [settingsData, profile] = await Promise.all([
          getMySettings(),
          getMyProfile(),
        ]);

        setSettings(settingsData);
        setPolicyLockedFields(settingsData.lockedFields ?? []);
        setIsAdmin(profile.role.toLowerCase() === "admin");
      } catch (requestError) {
        setError(
          requestError instanceof Error
            ? requestError.message
            : "Failed to load settings",
        );
      } finally {
        setIsLoading(false);
      }
    }

    loadData();
  }, []);

  const lockedSet = useMemo(
    () => new Set(settings?.lockedFields.map((field) => field.toLowerCase())),
    [settings],
  );

  const isLocked = (key: string) => lockedSet.has(key.toLowerCase());

  const handleToggle = (key: keyof ApiUserSettings) => {
    if (!settings || isLocked(String(key))) {
      return;
    }

    setSettings((prev) => {
      if (!prev) {
        return prev;
      }

      return { ...prev, [key]: !prev[key] };
    });
  };

  const handleSelectChange = (key: keyof ApiUserSettings, value: string) => {
    if (!settings || isLocked(String(key))) {
      return;
    }

    setSettings((prev) => (prev ? { ...prev, [key]: value } : prev));
  };

  const handleSave = async () => {
    if (!settings) {
      return;
    }

    setIsSaving(true);
    setError(null);

    try {
      const updated = await updateMySettings(settings);
      setSettings(updated);
      setPolicyLockedFields(updated.lockedFields ?? []);
    } catch (requestError) {
      setError(
        requestError instanceof Error
          ? requestError.message
          : "Failed to save settings",
      );
    } finally {
      setIsSaving(false);
    }
  };

  const togglePolicyField = (field: string) => {
    setPolicyLockedFields((prev) =>
      prev.includes(field)
        ? prev.filter((current) => current !== field)
        : [...prev, field],
    );
  };

  const handlePolicyUpdate = async () => {
    if (!policyTargetUid.trim()) {
      setError("Enter target user id to update policy");
      return;
    }

    setIsUpdatingPolicy(true);
    setError(null);

    try {
      await updateUserSettingsPolicy(policyTargetUid.trim(), {
        lockedFields: policyLockedFields,
      });
    } catch (requestError) {
      setError(
        requestError instanceof Error
          ? requestError.message
          : "Failed to update settings policy",
      );
    } finally {
      setIsUpdatingPolicy(false);
    }
  };

  if (isLoading || !settings) {
    return <div className="text-muted-foreground">Loading settings...</div>;
  }

  return (
    <div className="max-w-4xl mx-auto space-y-6">
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

      {error && (
        <div className="bg-destructive/10 border border-destructive/30 rounded-lg p-4 flex items-start gap-3">
          <AlertCircle className="w-5 h-5 text-destructive flex-shrink-0 mt-0.5" />
          <p className="text-sm text-destructive">{error}</p>
        </div>
      )}

      <div className="bg-card border border-border rounded-lg p-6 space-y-6">
        <div className="flex items-center gap-3">
          <Bell className="w-5 h-5 text-accent" />
          <h2 className="text-xl text-foreground">Notifications</h2>
        </div>

        <Separator />

        <div className="space-y-4">
          <SettingToggle
            id="emailNotifications"
            label="Email notifications"
            description="Receive notifications by email"
            checked={settings.emailNotifications}
            locked={isLocked("emailNotifications")}
            onChange={() => handleToggle("emailNotifications")}
          />

          <SettingToggle
            id="pushNotifications"
            label="Push notifications"
            description="Receive browser notifications"
            checked={settings.pushNotifications}
            locked={isLocked("pushNotifications")}
            onChange={() => handleToggle("pushNotifications")}
          />

          <SettingToggle
            id="smsNotifications"
            label="SMS notifications"
            description="Receive notifications by SMS"
            checked={settings.smsNotifications}
            locked={isLocked("smsNotifications")}
            onChange={() => handleToggle("smsNotifications")}
          />

          <Separator />

          <SettingToggle
            id="transactionAlerts"
            label="Transaction alerts"
            description="Notifications for every transaction"
            checked={settings.transactionAlerts}
            locked={isLocked("transactionAlerts")}
            onChange={() => handleToggle("transactionAlerts")}
          />

          <SettingToggle
            id="securityAlerts"
            label="Security alerts"
            description="Critical security notifications"
            checked={settings.securityAlerts}
            locked={isLocked("securityAlerts")}
            onChange={() => handleToggle("securityAlerts")}
          />

          <SettingToggle
            id="marketingEmails"
            label="Marketing emails"
            description="News and offers"
            checked={settings.marketingEmails}
            locked={isLocked("marketingEmails")}
            onChange={() => handleToggle("marketingEmails")}
          />
        </div>
      </div>

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
              disabled={isLocked("theme")}
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
              disabled={isLocked("language")}
            >
              <SelectTrigger id="language" className="bg-input-background">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="en">English</SelectItem>
                <SelectItem value="es">Espanol</SelectItem>
                <SelectItem value="zh">Chinese</SelectItem>
              </SelectContent>
            </Select>
          </div>
        </div>
      </div>

      <div className="bg-card border border-border rounded-lg p-6 space-y-6">
        <div className="flex items-center gap-3">
          <Shield className="w-5 h-5 text-accent" />
          <h2 className="text-xl text-foreground">Privacy and security</h2>
        </div>

        <Separator />

        <div className="space-y-4">
          <SettingToggle
            id="publicProfile"
            label="Public profile"
            description="Make profile visible to others"
            checked={settings.publicProfile}
            locked={isLocked("publicProfile")}
            onChange={() => handleToggle("publicProfile")}
          />

          <SettingToggle
            id="showActivity"
            label="Show activity"
            description="Display your activity"
            checked={settings.showActivity}
            locked={isLocked("showActivity")}
            onChange={() => handleToggle("showActivity")}
          />

          <SettingToggle
            id="dataCollection"
            label="Data collection for analytics"
            description="Helps improve the service"
            checked={settings.dataCollection}
            locked={isLocked("dataCollection")}
            onChange={() => handleToggle("dataCollection")}
          />

          <SettingToggle
            id="twoFactorAuth"
            label="Two-factor authentication"
            description="Extra layer of protection"
            checked={settings.twoFactorAuth}
            locked={isLocked("twoFactorAuth")}
            onChange={() => handleToggle("twoFactorAuth")}
          />

          <SettingToggle
            id="biometric"
            label="Biometric authentication"
            description="Sign in with fingerprint or Face ID"
            checked={settings.biometric}
            locked={isLocked("biometric")}
            onChange={() => handleToggle("biometric")}
          />

          <div className="space-y-2">
            <Label htmlFor="sessionTimeout">Session timeout</Label>
            <Select
              value={settings.sessionTimeout}
              onValueChange={(value) =>
                handleSelectChange("sessionTimeout", value)
              }
              disabled={isLocked("sessionTimeout")}
            >
              <SelectTrigger
                id="sessionTimeout"
                className="bg-input-background"
              >
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

      {isAdmin && (
        <div className="bg-card border border-border rounded-lg p-6 space-y-4">
          <h2 className="text-xl text-foreground">Admin policy controls</h2>
          <p className="text-sm text-muted-foreground">
            Lock selected settings fields for a target user.
          </p>

          <div className="space-y-2">
            <Label htmlFor="policyTargetUid">Target user id</Label>
            <input
              id="policyTargetUid"
              value={policyTargetUid}
              onChange={(event) => setPolicyTargetUid(event.target.value)}
              className="w-full h-10 px-3 bg-input-background border border-input rounded-lg"
              placeholder="firebase uid"
            />
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-2">
            {POLICY_FIELDS.map((field) => (
              <label
                key={field}
                className="flex items-center gap-2 text-sm text-foreground"
              >
                <input
                  type="checkbox"
                  checked={policyLockedFields.includes(field)}
                  onChange={() => togglePolicyField(field)}
                />
                {field}
              </label>
            ))}
          </div>

          <Button onClick={handlePolicyUpdate} disabled={isUpdatingPolicy}>
            {isUpdatingPolicy ? "Applying..." : "Apply policy"}
          </Button>
        </div>
      )}

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
              location: "Current location",
              lastActive: "Active now",
              current: true,
            },
            {
              icon: Smartphone,
              name: "Mobile session",
              location: "Synced",
              lastActive: "Recent",
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
                      {device.location} · {device.lastActive}
                    </p>
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      </div>

      <div className="bg-card border border-border rounded-lg p-6 space-y-3">
        <Button variant="outline" className="w-full justify-start">
          <Download className="w-4 h-4 mr-2" />
          Download my data
        </Button>

        <Button
          variant="outline"
          className="w-full justify-start text-destructive hover:text-destructive hover:bg-destructive/10"
        >
          <Trash2 className="w-4 h-4 mr-2" />
          Delete account
        </Button>

        <div className="bg-accent/10 border border-accent/30 rounded-lg p-4 mt-4">
          <div className="flex items-start gap-3">
            <Mail className="w-5 h-5 text-accent flex-shrink-0 mt-0.5" />
            <div>
              <p className="text-sm text-accent-foreground/90">
                <strong>Need help?</strong>
              </p>
              <p className="text-sm text-muted-foreground mt-1">
                Contact support at support@financehub.com
              </p>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

type SettingToggleProps = {
  id: string;
  label: string;
  description: string;
  checked: boolean;
  locked: boolean;
  onChange: () => void;
};

function SettingToggle({
  id,
  label,
  description,
  checked,
  locked,
  onChange,
}: SettingToggleProps) {
  return (
    <div className="flex items-center justify-between">
      <div className="space-y-0.5">
        <Label htmlFor={id}>{label}</Label>
        <p className="text-sm text-muted-foreground">
          {description}
          {locked ? " (locked by policy)" : ""}
        </p>
      </div>
      <Switch
        id={id}
        checked={checked}
        disabled={locked}
        onCheckedChange={onChange}
      />
    </div>
  );
}
