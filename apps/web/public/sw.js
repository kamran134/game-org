// Минимальный service worker под Web Push (docs/PLAN.md, Шаг 17). Ничего не
// кэширует — только показывает push-уведомления и открывает ссылку по клику.

self.addEventListener("push", (event) => {
  if (!event.data) return;

  let payload;
  try {
    payload = event.data.json();
  } catch {
    payload = { title: "game.org.az", body: event.data.text() };
  }

  const title = payload.title || "game.org.az";
  const options = {
    body: payload.body || "",
    icon: "/icon.svg",
    data: payload.data || {},
  };

  event.waitUntil(self.registration.showNotification(title, options));
});

self.addEventListener("notificationclick", (event) => {
  event.notification.close();

  const url = (event.notification.data && event.notification.data.url) || "/";

  event.waitUntil(
    self.clients.matchAll({ type: "window", includeUncontrolled: true }).then((clientList) => {
      for (const client of clientList) {
        if (client.url === url && "focus" in client) return client.focus();
      }
      if (self.clients.openWindow) return self.clients.openWindow(url);
    }),
  );
});
