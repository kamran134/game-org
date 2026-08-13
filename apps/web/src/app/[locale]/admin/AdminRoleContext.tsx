"use client";

import { createContext, useContext } from "react";
import type { UserRole } from "@/lib/authApi";

// Сервер — источник истины по правам (каждый мутирующий эндпоинт сам
// проверяет Moderator/Admin), этот контекст только прячет то, что текущий
// пользователь всё равно не смог бы выполнить: сменить роль может только
// Admin (Шаг 23 плана — "скрытая кнопка — не проверка").
export type AdminViewer = { userId: string; role: UserRole };

const AdminRoleContext = createContext<AdminViewer | null>(null);

export const AdminRoleProvider = AdminRoleContext.Provider;

export function useAdminViewer(): AdminViewer | null {
  return useContext(AdminRoleContext);
}
