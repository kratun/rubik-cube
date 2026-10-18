interface AlertBannerProps {
  message: string;
}

/** An error or a "changed by someone else" message, set apart from the content (role="alert"). */
export function AlertBanner({ message }: AlertBannerProps) {
  return (
    <p role="alert" className="alert-banner">
      {message}
    </p>
  );
}
