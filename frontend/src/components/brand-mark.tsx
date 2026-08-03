type BrandMarkProps = {
  compact?: boolean;
};

export function BrandMark({ compact = false }: BrandMarkProps) {
  return (
    <div className="brand" aria-label="SiPacul">
      <span className="brand__mark" aria-hidden="true">
        <svg viewBox="0 0 48 48" role="img">
          <path d="M14 36V15.5C14 10.8 17.8 7 22.5 7H27c6.1 0 11 4.9 11 11s-4.9 11-11 11h-6" />
          <path d="M11 39c8-1.2 14.5-4.7 19.5-10.5" />
          <path d="M9 39h10" />
        </svg>
      </span>
      {!compact && (
        <span className="brand__copy">
          <strong>SiPacul</strong>
          <small>Sistem Pencatatan Akuntansi Usaha Lestari</small>
        </span>
      )}
    </div>
  );
}