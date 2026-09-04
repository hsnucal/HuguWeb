export function workplacePropertyBannerRequired(pathname: string): boolean {
  return pathname.startsWith('/app/room-operations') || pathname.startsWith('/app/technical-service')
}
