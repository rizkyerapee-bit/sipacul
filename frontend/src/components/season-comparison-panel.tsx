import type {
  ComparisonValue,
  SeasonComparison,
  SeasonComparisonRow,
} from "@/lib/evaluations/season-comparison";
import {
  formatSeasonCurrency,
  formatSeasonDate,
} from "@/lib/evaluations/season-history";
import styles from "./season-comparison-panel.module.css";

type Props = {
  comparison: SeasonComparison | null;
  selectedCount: number;
  onClear: () => void;
};

const numberFormatter = new Intl.NumberFormat("id-ID", {
  maximumFractionDigits: 2,
});

function formattedNumber(value: number, unit: SeasonComparisonRow["unit"]): string {
  if (unit === "currency") return formatSeasonCurrency(value);
  if (unit === "percentage") return `${numberFormatter.format(value)}%`;
  if (unit === "days") return `${numberFormatter.format(value)} hari`;
  return numberFormatter.format(value);
}

function valueLabel(value: number | null, unit: SeasonComparisonRow["unit"]): string {
  return value === null ? "Belum tersedia" : formattedNumber(value, unit);
}

function deltaLabel(value: ComparisonValue, unit: SeasonComparisonRow["unit"]): string {
  if (value.deltaFromBaseline === null) return "Delta tidak tersedia";
  if (value.deltaFromBaseline === 0) return "Tidak berubah";
  const prefix = value.deltaFromBaseline > 0 ? "+" : "−";
  return `${prefix}${formattedNumber(Math.abs(value.deltaFromBaseline), unit)} dari baseline`;
}

export function SeasonComparisonPanel({ comparison, selectedCount, onClear }: Props) {
  if (!comparison) {
    return (
      <section className={styles.emptyPanel} aria-label="Perbandingan lintas musim">
        <div>
          <span>Perbandingan lintas musim</span>
          <strong>{selectedCount === 0 ? "Pilih musim dari panel detail" : `${selectedCount} musim dipilih`}</strong>
          <small>Pilih minimal dua dan maksimal empat musim selesai pada halaman ini.</small>
        </div>
        {selectedCount > 0 && <button type="button" onClick={onClear}>Hapus pilihan</button>}
      </section>
    );
  }

  return (
    <section className={styles.panel} aria-label="Perbandingan lintas musim">
      <header>
        <div><span>Fakta deterministik</span><h2>Perbandingan {comparison.columns.length} musim</h2><p>Kolom pertama adalah baseline terlama. Arah delta tidak menilai perubahan sebagai baik atau buruk.</p></div>
        <button type="button" onClick={onClear}>Hapus pilihan</button>
      </header>

      {(!comparison.sameCommodity || !comparison.sameLandPlot) && (
        <div className={styles.contextWarning} role="note">
          <strong>Konteks pilihan berbeda.</strong>
          <span>{!comparison.sameCommodity ? "Komoditas tidak sama. " : ""}{!comparison.sameLandPlot ? "Petak tidak sama. " : ""}Interpretasikan delta bersama kondisi agronomis masing-masing musim.</span>
        </div>
      )}

      <div className={styles.tableWrap}>
        <table>
          <thead>
            <tr>
              <th scope="col">Fakta</th>
              {comparison.columns.map((column, index) => (
                <th scope="col" key={column.cropCycleId}>
                  <span>{index === 0 ? "Baseline" : `Musim ${index + 1}`}</span>
                  <strong>{column.cropCycleName}</strong>
                  <small>{column.cropCycleCode} · {column.commodityName}</small>
                  <small>{column.landPlotName} · {formatSeasonDate(column.comparisonDate)}</small>
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {comparison.rows.map((row) => (
              <tr key={row.key}>
                <th scope="row">{row.label}</th>
                {row.values.map((value, index) => (
                  <td key={comparison.columns[index].cropCycleId}>
                    <strong>{valueLabel(value.value, row.unit)}</strong>
                    <small className={styles[value.direction]}>{index === 0 ? "Nilai baseline" : deltaLabel(value, row.unit)}</small>
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}

export const seasonComparisonSelectionClassNames = {
  actions: styles.detailActions,
  button: styles.selectionButton,
  selected: styles.selectionButtonSelected,
};
