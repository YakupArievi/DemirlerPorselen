import { useEffect, useRef } from 'react';
import { Modal, Pressable, Text, View } from 'react-native';
import { CameraView, useCameraPermissions } from 'expo-camera';
import { Btn, colors } from '../ui';

/**
 * QR okutma modalı. Açılınca kamera ile QR taranır; ilk okumada onScan(data) çağrılır.
 * data = ürünün QR/kod değeri (adet veya koli kodu). Çağıran bunu /variants/resolve ile çözer.
 */
export function QrScanner({ visible, onScan, onClose }: {
  visible: boolean; onScan: (data: string) => void; onClose: () => void;
}) {
  const [perm, requestPerm] = useCameraPermissions();
  const locked = useRef(false);

  useEffect(() => { if (visible) locked.current = false; }, [visible]);

  return (
    <Modal visible={visible} animationType="slide" onRequestClose={onClose}>
      <View style={{ flex: 1, backgroundColor: '#000' }}>
        <View style={{ flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', padding: 16, paddingTop: 28 }}>
          <Text style={{ color: '#fff', fontSize: 18, fontWeight: 'bold' }}>QR Okut</Text>
          <Pressable onPress={onClose} hitSlop={12}><Text style={{ color: '#fff', fontSize: 20 }}>✕</Text></Pressable>
        </View>

        {!perm ? (
          <View style={{ flex: 1, justifyContent: 'center', alignItems: 'center' }}><Text style={{ color: '#fff' }}>Kamera hazırlanıyor...</Text></View>
        ) : !perm.granted ? (
          <View style={{ flex: 1, justifyContent: 'center', alignItems: 'center', padding: 24 }}>
            <Text style={{ color: '#fff', textAlign: 'center', marginBottom: 16 }}>QR okutmak için kamera izni gerekiyor.</Text>
            <Btn title="Kamera İzni Ver" onPress={requestPerm} />
          </View>
        ) : (
          <View style={{ flex: 1 }}>
            <CameraView
              style={{ flex: 1 }}
              barcodeScannerSettings={{ barcodeTypes: ['qr'] }}
              onBarcodeScanned={(r) => {
                if (locked.current) return;
                locked.current = true;
                onScan(r.data);
              }}
            />
            <View style={{ position: 'absolute', bottom: 40, left: 0, right: 0, alignItems: 'center' }}>
              <Text style={{ color: '#fff', backgroundColor: '#0008', paddingHorizontal: 14, paddingVertical: 8, borderRadius: 20 }}>
                QR kodu çerçeveye getirin
              </Text>
            </View>
            {/* basit nişangah */}
            <View pointerEvents="none" style={{ position: 'absolute', top: '30%', left: '15%', width: '70%', height: '32%', borderWidth: 2, borderColor: colors.accent2, borderRadius: 16 }} />
          </View>
        )}
      </View>
    </Modal>
  );
}
