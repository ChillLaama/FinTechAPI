import {
  useCallback,
  useEffect,
  useRef,
  useState,
  startTransition,
} from "react";
import {
  Bell,
  Check,
  CheckCheck,
  ShieldAlert,
  CreditCard,
  AlertTriangle,
} from "lucide-react";
import { Popover, PopoverContent, PopoverTrigger } from "./ui/popover";
import {
  getNotifications,
  getUnreadNotificationCount,
  markNotificationAsRead,
  markAllNotificationsAsRead,
  type ApiNotification,
} from "../api/client";

const POLL_INTERVAL = 30_000;

const TYPE_ICONS: Record<string, typeof Bell> = {
  payment_succeeded: CreditCard,
  payment_failed: AlertTriangle,
  fraud_block: ShieldAlert,
  fraud_review: ShieldAlert,
  fraud_resolved: ShieldAlert,
};

function timeAgo(dateStr: string): string {
  const seconds = Math.floor((Date.now() - new Date(dateStr).getTime()) / 1000);
  if (seconds < 60) return "just now";
  const minutes = Math.floor(seconds / 60);
  if (minutes < 60) return `${minutes}m ago`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours}h ago`;
  const days = Math.floor(hours / 24);
  return `${days}d ago`;
}

export function NotificationBell() {
  const [notifications, setNotifications] = useState<ApiNotification[]>([]);
  const [unreadCount, setUnreadCount] = useState(0);
  const [isOpen, setIsOpen] = useState(false);
  const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null);

  const fetchNotifications = useCallback(async () => {
    try {
      const data = await getNotifications(30);
      startTransition(() => {
        setNotifications(data);
        setUnreadCount(data.filter((n) => !n.isRead).length);
      });
    } catch {
      // silent
    }
  }, []);

  useEffect(() => {
    let cancelled = false;
    const poll = async () => {
      try {
        const { count } = await getUnreadNotificationCount();
        if (!cancelled) startTransition(() => setUnreadCount(count));
      } catch {
        // silent — polling should not disrupt UX
      }
    };
    poll();
    intervalRef.current = setInterval(poll, POLL_INTERVAL);
    return () => {
      cancelled = true;
      if (intervalRef.current) clearInterval(intervalRef.current);
    };
  }, []);

  useEffect(() => {
    if (isOpen) {
      fetchNotifications();
    }
  }, [isOpen, fetchNotifications]);

  const handleMarkAsRead = async (id: string) => {
    await markNotificationAsRead(id);
    setNotifications((prev) =>
      prev.map((n) => (n.id === id ? { ...n, isRead: true } : n)),
    );
    setUnreadCount((prev) => Math.max(0, prev - 1));
  };

  const handleMarkAllAsRead = async () => {
    await markAllNotificationsAsRead();
    setNotifications((prev) => prev.map((n) => ({ ...n, isRead: true })));
    setUnreadCount(0);
  };

  return (
    <Popover open={isOpen} onOpenChange={setIsOpen}>
      <PopoverTrigger asChild>
        <button className="relative p-2 rounded-lg hover:bg-secondary transition-colors">
          <Bell className="w-5 h-5 text-muted-foreground" />
          {unreadCount > 0 && (
            <span className="absolute -top-0.5 -right-0.5 flex items-center justify-center min-w-[18px] h-[18px] px-1 text-[10px] font-bold text-white bg-destructive rounded-full">
              {unreadCount > 99 ? "99+" : unreadCount}
            </span>
          )}
        </button>
      </PopoverTrigger>

      <PopoverContent
        align="end"
        sideOffset={8}
        className="w-96 p-0 max-h-[480px] flex flex-col"
      >
        <div className="flex items-center justify-between px-4 py-3 border-b border-border">
          <h3 className="text-sm font-semibold text-foreground">
            Notifications
          </h3>
          {unreadCount > 0 && (
            <button
              onClick={handleMarkAllAsRead}
              className="flex items-center gap-1 text-xs text-accent hover:text-accent/80 transition-colors"
            >
              <CheckCheck className="w-3.5 h-3.5" />
              Mark all read
            </button>
          )}
        </div>

        <div className="flex-1 overflow-y-auto">
          {notifications.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-10 text-muted-foreground">
              <Bell className="w-8 h-8 mb-2 opacity-40" />
              <p className="text-sm">No notifications yet</p>
            </div>
          ) : (
            notifications.map((notification) => {
              const Icon = TYPE_ICONS[notification.type] ?? Bell;
              return (
                <button
                  key={notification.id}
                  onClick={() => {
                    if (!notification.isRead) handleMarkAsRead(notification.id);
                  }}
                  className={`w-full text-left px-4 py-3 border-b border-border/50 hover:bg-secondary/40 transition-colors flex gap-3 ${
                    notification.isRead ? "opacity-60" : ""
                  }`}
                >
                  <div className="flex-shrink-0 mt-0.5">
                    <Icon
                      className={`w-4 h-4 ${
                        notification.isRead
                          ? "text-muted-foreground"
                          : "text-accent"
                      }`}
                    />
                  </div>
                  <div className="flex-1 min-w-0">
                    <div className="flex items-start justify-between gap-2">
                      <p
                        className={`text-sm leading-tight ${
                          notification.isRead
                            ? "text-muted-foreground"
                            : "text-foreground font-medium"
                        }`}
                      >
                        {notification.title}
                      </p>
                      {!notification.isRead && (
                        <Check className="w-3.5 h-3.5 text-accent flex-shrink-0 mt-0.5" />
                      )}
                    </div>
                    <p className="text-xs text-muted-foreground mt-0.5 line-clamp-2">
                      {notification.message}
                    </p>
                    <p className="text-[10px] text-muted-foreground/60 mt-1">
                      {timeAgo(notification.createdAt)}
                    </p>
                  </div>
                </button>
              );
            })
          )}
        </div>
      </PopoverContent>
    </Popover>
  );
}
