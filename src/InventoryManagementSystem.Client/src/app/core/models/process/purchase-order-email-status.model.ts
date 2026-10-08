export interface PurchaseOrderEmailStatusModel {
  id: string;
  status: 'Pending' | 'Sending' | 'Sent' | 'Failed';
  attempts: number;
  sentOn: string | null;
  nextAttemptOn: string | null;
}
