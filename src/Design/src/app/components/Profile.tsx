import { useState } from "react";
import {
  User,
  Mail,
  Phone,
  Calendar,
  MapPin,
  Shield,
  Camera,
  Edit2,
  Save,
  X,
  CheckCircle2,
} from "lucide-react";
import { Button } from "./ui/button";
import { Input } from "./ui/input";
import { Label } from "./ui/label";
import { Badge } from "./ui/badge";

export function Profile() {
  const [isEditing, setIsEditing] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [profileData, setProfileData] = useState({
    fullName: "Александр Петров",
    email: "demo@financehub.com",
    phone: "+7 (999) 123-45-67",
    location: "Москва, Россия",
    dateJoined: "15 января 2024",
    accountType: "Premium",
    verified: true,
  });

  const [editData, setEditData] = useState({ ...profileData });

  const handleEdit = () => {
    setIsEditing(true);
    setEditData({ ...profileData });
  };

  const handleCancel = () => {
    setIsEditing(false);
    setEditData({ ...profileData });
  };

  const handleSave = () => {
    setIsSaving(true);
    // Имитация сохранения
    setTimeout(() => {
      setProfileData({ ...editData });
      setIsEditing(false);
      setIsSaving(false);
    }, 1000);
  };

  const handleChange = (field: string, value: string) => {
    setEditData((prev) => ({ ...prev, [field]: value }));
  };

  return (
    <div className="max-w-4xl mx-auto space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl text-foreground">Профиль</h1>
          <p className="text-muted-foreground mt-1">
            Управляйте информацией о вашем аккаунте
          </p>
        </div>

        {!isEditing ? (
          <Button onClick={handleEdit}>
            <Edit2 className="w-4 h-4 mr-2" />
            Редактировать
          </Button>
        ) : (
          <div className="flex gap-2">
            <Button variant="outline" onClick={handleCancel}>
              <X className="w-4 h-4 mr-2" />
              Отменить
            </Button>
            <Button onClick={handleSave} disabled={isSaving}>
              <Save className="w-4 h-4 mr-2" />
              {isSaving ? "Сохранение..." : "Сохранить"}
            </Button>
          </div>
        )}
      </div>

      <div className="grid gap-6">
        {/* Profile Card */}
        <div className="bg-card border border-border rounded-lg p-6">
          <div className="flex items-start gap-6">
            {/* Avatar */}
            <div className="relative group">
              <div className="w-24 h-24 rounded-full bg-gradient-to-br from-primary to-accent flex items-center justify-center text-3xl text-white">
                АП
              </div>
              {isEditing && (
                <button className="absolute inset-0 rounded-full bg-black/50 flex items-center justify-center opacity-0 group-hover:opacity-100 transition-opacity">
                  <Camera className="w-6 h-6 text-white" />
                </button>
              )}
            </div>

            {/* Info */}
            <div className="flex-1 space-y-4">
              <div className="flex items-center gap-3">
                <h2 className="text-2xl text-foreground">
                  {profileData.fullName}
                </h2>
                {profileData.verified && (
                  <Badge className="bg-success/10 text-success border-success/30">
                    <CheckCircle2 className="w-3 h-3 mr-1" />
                    Верифицирован
                  </Badge>
                )}
                <Badge className="bg-accent/10 text-accent border-accent/30">
                  {profileData.accountType}
                </Badge>
              </div>

              <div className="grid grid-cols-2 gap-4 text-sm">
                <div className="flex items-center gap-2 text-muted-foreground">
                  <Mail className="w-4 h-4" />
                  <span>{profileData.email}</span>
                </div>
                <div className="flex items-center gap-2 text-muted-foreground">
                  <Phone className="w-4 h-4" />
                  <span>{profileData.phone}</span>
                </div>
                <div className="flex items-center gap-2 text-muted-foreground">
                  <MapPin className="w-4 h-4" />
                  <span>{profileData.location}</span>
                </div>
                <div className="flex items-center gap-2 text-muted-foreground">
                  <Calendar className="w-4 h-4" />
                  <span>С нами с {profileData.dateJoined}</span>
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Edit Form */}
        {isEditing && (
          <div className="bg-card border border-border rounded-lg p-6 space-y-4">
            <h3 className="text-lg text-foreground mb-4">Редактировать профиль</h3>

            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="fullName">Полное имя</Label>
                <div className="relative">
                  <User className="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-muted-foreground" />
                  <Input
                    id="fullName"
                    value={editData.fullName}
                    onChange={(e) => handleChange("fullName", e.target.value)}
                    className="pl-10 bg-input-background"
                  />
                </div>
              </div>

              <div className="space-y-2">
                <Label htmlFor="email">Email</Label>
                <div className="relative">
                  <Mail className="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-muted-foreground" />
                  <Input
                    id="email"
                    type="email"
                    value={editData.email}
                    onChange={(e) => handleChange("email", e.target.value)}
                    className="pl-10 bg-input-background"
                  />
                </div>
              </div>

              <div className="space-y-2">
                <Label htmlFor="phone">Телефон</Label>
                <div className="relative">
                  <Phone className="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-muted-foreground" />
                  <Input
                    id="phone"
                    value={editData.phone}
                    onChange={(e) => handleChange("phone", e.target.value)}
                    className="pl-10 bg-input-background"
                  />
                </div>
              </div>

              <div className="space-y-2">
                <Label htmlFor="location">Местоположение</Label>
                <div className="relative">
                  <MapPin className="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-muted-foreground" />
                  <Input
                    id="location"
                    value={editData.location}
                    onChange={(e) => handleChange("location", e.target.value)}
                    className="pl-10 bg-input-background"
                  />
                </div>
              </div>
            </div>
          </div>
        )}

        {/* Security Section */}
        <div className="bg-card border border-border rounded-lg p-6">
          <div className="flex items-center gap-3 mb-4">
            <Shield className="w-5 h-5 text-accent" />
            <h3 className="text-lg text-foreground">Безопасность</h3>
          </div>

          <div className="space-y-4">
            <div className="flex items-center justify-between py-3 border-b border-border last:border-0">
              <div>
                <p className="text-foreground">Пароль</p>
                <p className="text-sm text-muted-foreground">
                  Последнее изменение 30 дней назад
                </p>
              </div>
              <Button variant="outline" size="sm">
                Изменить
              </Button>
            </div>

            <div className="flex items-center justify-between py-3 border-b border-border last:border-0">
              <div>
                <p className="text-foreground">Двухфакторная аутентификация</p>
                <p className="text-sm text-muted-foreground">
                  Защитите аккаунт с помощью 2FA
                </p>
              </div>
              <Button variant="outline" size="sm">
                Настроить
              </Button>
            </div>

            <div className="flex items-center justify-between py-3">
              <div>
                <p className="text-foreground">Активные сессии</p>
                <p className="text-sm text-muted-foreground">
                  Управляйте устройствами с доступом к аккаунту
                </p>
              </div>
              <Button variant="outline" size="sm">
                Посмотреть
              </Button>
            </div>
          </div>
        </div>

        {/* Activity Stats */}
        <div className="grid grid-cols-3 gap-4">
          <div className="bg-card border border-border rounded-lg p-4">
            <p className="text-sm text-muted-foreground mb-1">
              Транзакций
            </p>
            <p className="text-2xl text-foreground">1,247</p>
            <p className="text-xs text-success mt-1">+12% за месяц</p>
          </div>

          <div className="bg-card border border-border rounded-lg p-4">
            <p className="text-sm text-muted-foreground mb-1">
              Общий объём
            </p>
            <p className="text-2xl text-foreground">₽2.4M</p>
            <p className="text-xs text-success mt-1">+8% за месяц</p>
          </div>

          <div className="bg-card border border-border rounded-lg p-4">
            <p className="text-sm text-muted-foreground mb-1">
              Успешность
            </p>
            <p className="text-2xl text-foreground">98.5%</p>
            <p className="text-xs text-muted-foreground mt-1">
              Средний показатель
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}
