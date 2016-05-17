using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentoContable
{
    class Documento
    {
        public String noSociedad;
        public String fechaRecepcion;
        public String fechaContabilizacion;
        public String tipoDocumento;
        public String mesContabilizacion;
        public String tipoMoneda;
        public String identificadorDocumento;
        public String encabezadoDocumento;
        public String noAcreedor;
        public String total;
        public String indicadorIva;
        public String noCuentaContable;
        public String subtotal;
        public String centroCostos;
        public String descripcionDocumento;
        public String cuentaMayor;
        public String totalImpuestos;
        public String uuid;
        public int facturaId;
        public int enviado;
        public int noDocumento;
        public String error;
        public int preliminar;
        public Documento(String noSociedad,String fechaRecepcion,String tipoDoc,String 
            mesCont,String tipoMoneda,String identificadorDoc,String noAcreedor,String total,
            String indicadorIva,String noCuenta,String subtotal,String centroCostos,String descripcion,String cuentaMayor,
            String totalImp,String uuid,int facturaId,int preliminar)
        {
            this.noSociedad = noSociedad;
            this.fechaRecepcion = fechaRecepcion;
            this.fechaContabilizacion = fechaRecepcion;
            this.tipoDocumento = tipoDoc;
            this.mesContabilizacion = mesCont;
            this.tipoMoneda = tipoMoneda;
            this.identificadorDocumento = identificadorDoc;
            this.encabezadoDocumento = identificadorDoc;
            //Por formato de SAP el No. Acreedor debe tener 10 digitos
            //se rellena con ceros antes
            if (noAcreedor.Length < 10 && Char.IsDigit(noAcreedor,0) )
            {
                for (int i = noAcreedor.Length; i < 10; i++)
                {
                    noAcreedor = "0" + noAcreedor;
                }
            }
            this.noAcreedor = noAcreedor;
            this.total = total;
            this.indicadorIva = indicadorIva;
            this.noCuentaContable = noCuenta;
            this.subtotal = subtotal;
            //Por formato de SAP centro de costos debe tener 10 digitos
            //se rellena con ceros antes
            if (centroCostos.Length <10){
                for (int i = centroCostos.Length; i < 10; i++)
                {
                    centroCostos = "0" + centroCostos;
                }
            }
            this.centroCostos = centroCostos;
            this.descripcionDocumento = descripcion;
            this.cuentaMayor = cuentaMayor;
            this.totalImpuestos = totalImp;
            this.uuid = uuid;
            this.facturaId = facturaId;
            this.preliminar = preliminar;
        }
    }
    
}
