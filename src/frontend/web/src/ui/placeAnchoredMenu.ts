export function placeAnchoredMenu(
  trigger: { top: number; left: number; right: number; bottom: number; width: number },
  menu: { width: number; height: number },
  viewport: { width: number; height: number },
  gap = 6,
  pad = 8,
): { top: number; left: number; width: number } {
  const width = Math.min(menu.width, Math.max(viewport.width - pad * 2, 0))
  let left = trigger.left
  if (left + width > viewport.width - pad) {
    left = trigger.right - width
  }
  if (left < pad) {
    left = pad
  }

  let top = trigger.bottom + gap
  if (top + menu.height > viewport.height - pad) {
    top = trigger.top - menu.height - gap
  }
  if (top < pad) {
    top = pad
  }

  return { top, left, width }
}
