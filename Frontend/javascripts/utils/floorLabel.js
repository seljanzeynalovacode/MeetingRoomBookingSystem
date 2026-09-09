import { t } from "../components/i18n.js";

export function floorLabel(levelNumber) {
  return `${t("adminbuildings.floor_label")}: ${levelNumber}`;
}
