import { useEffect, useMemo, useState } from "react";
import { Moon, Shield, Save, AlertCircle, Bell } from "lucide-react";
import { Button } from "./ui/button";
import { Label } from "./ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "./ui/select";
import { Separator } from "./ui/separator";
import { Switch } from "./ui/switch";
import {
  getMyProfile,
  getMySettings,
  updateMySettings,
  updateUserSettingsPolicy,
  type ApiUserSettings,
} from "../api/client";

const POLICY_FIELDS = [
  "theme",
  "language",
  "defaultCurrency",
  "transactionNotifications",
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

  const handleSelectChange = (key: keyof ApiUserSettings, value: string) => {
    if (!settings || isLocked(String(key))) {
      return;
    }

    setSettings((prev) => (prev ? { ...prev, [key]: value } : prev));
  };

  const handleToggle = (key: keyof ApiUserSettings, value: boolean) => {
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
            {isLocked("theme") && (
              <p className="text-xs text-muted-foreground">
                Locked by admin policy
              </p>
            )}
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
                <SelectItem value="es">Español</SelectItem>
                <SelectItem value="zh">Chinese</SelectItem>
              </SelectContent>
            </Select>
            {isLocked("language") && (
              <p className="text-xs text-muted-foreground">
                Locked by admin policy
              </p>
            )}
          </div>

          <div className="space-y-2">
            <Label htmlFor="defaultCurrency">Default currency</Label>
            <Select
              value={settings.defaultCurrency}
              onValueChange={(value) =>
                handleSelectChange("defaultCurrency", value)
              }
              disabled={isLocked("defaultCurrency")}
            >
              <SelectTrigger
                id="defaultCurrency"
                className="bg-input-background"
              >
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="usd">USD ($)</SelectItem>
                <SelectItem value="eur">EUR (€)</SelectItem>
                <SelectItem value="gbp">GBP (£)</SelectItem>
              </SelectContent>
            </Select>
            {isLocked("defaultCurrency") && (
              <p className="text-xs text-muted-foreground">
                Locked by admin policy
              </p>
            )}
          </div>
        </div>
      </div>

      <div className="bg-card border border-border rounded-lg p-6 space-y-6">
        <div className="flex items-center gap-3">
          <Bell className="w-5 h-5 text-accent" />
          <h2 className="text-xl text-foreground">Notifications</h2>
        </div>

        <Separator />

        <div className="flex items-center justify-between">
          <div className="space-y-0.5">
            <Label htmlFor="transactionNotifications">
              Transaction notifications
            </Label>
            <p className="text-xs text-muted-foreground">
              Receive notifications for payments, fraud blocks, and case
              resolutions
            </p>
          </div>
          <Switch
            id="transactionNotifications"
            checked={settings.transactionNotifications}
            onCheckedChange={(value) =>
              handleToggle("transactionNotifications", value)
            }
            disabled={isLocked("transactionNotifications")}
          />
        </div>
        {isLocked("transactionNotifications") && (
          <p className="text-xs text-muted-foreground">
            Locked by admin policy
          </p>
        )}
      </div>

      <div className="bg-card border border-border rounded-lg p-6 space-y-6">
        <div className="flex items-center gap-3">
          <Shield className="w-5 h-5 text-accent" />
          <h2 className="text-xl text-foreground">Security</h2>
        </div>

        <Separator />

        <div className="space-y-4">
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
            {isLocked("sessionTimeout") && (
              <p className="text-xs text-muted-foreground">
                Locked by admin policy
              </p>
            )}
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
    </div>
  );
}
