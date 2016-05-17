using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MySql.Data.MySqlClient;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Net;
using System.ServiceModel;

/* Autora: Ana Gpe. Arellano Palacios
 * Mundo Inmobiliario S.A
 * Descripción: Este desarrollo toma la información validada y autorizada de una solicitud de egresos
 * digital, generada atraves del portal de recepción de facturas electronicas. Se considera el ultimo paso
 * del RFE y usa una conexión estilo webservice para comunicarse con SAP, generando un documento contable
 * o documento preliminar en la transacción FB60.
 */
namespace DocumentoContable
{
    class Program
    {

        private static MySqlConnection conectar()
        {
            MySqlConnection mySqlConn = new MySqlConnection();

            return mySqlConn;
        }
        public static void actualizaRegistro(Documento[] documentos)
        {
            MySqlConnection conexion = conectar();
            conexion.Open();
            foreach (Documento documento in documentos)
            {
                if (documento != null)
                {
                    String query = "UPDATE factaprobada_cuentacontable SET enviado =" + documento.enviado + ", error = '" + documento.error +
                        "',no_documento=" + documento.noDocumento + " WHERE fa_fkey =" + documento.facturaId + ";";
                    try
                    {
                        MySqlCommand cmd = new MySqlCommand(query, conexion);
                        cmd.ExecuteNonQuery();
                    }
                    catch (MySqlException ex)
                    {
                        Console.WriteLine("[DBUPDATE] " + ex.Message);
                    }
                }
            }
            conexion.Dispose();
            conexion.Close();
        }

        public static int totalPendientes()
        {
            int contador = 0;
            MySqlConnection conexion = conectar();
            conexion.Open();
            //Revisa cuantos faltan por enviar
            //sin duplicar
            String query = "SELECT COUNT(*)" +
                " FROM factaprobada_cuentacontable factcc " +
                "INNER JOIN superuserfact suf ON suf.factauto_fkey = factcc.fa_fkey "+
                "INNER JOIN fact_autorizada f ON f.factauto_pkey = factcc.fa_fkey " +
                "INNER JOIN TFD_TIMBREFISCALDIGITAL TIMBRE ON f.fact_fkey = TIMBRE.UUID " +
                "WHERE factcc.acepto=1 AND factcc.aprobo=1 AND factcc.enviado=0 " +
                "AND STR_TO_DATE(TIMBRE.FECHATIMBRADO,'%Y-%m-%d') > DATE_SUB(CURDATE(),INTERVAL 40 DAY) AND "+
                "suf.APROBO = 1 AND ((suf.super_user_fkey IS NULL) OR (suf.APROBO2=1)) ";
            try
            {
                MySqlCommand cmd = new MySqlCommand(query, conexion);
                MySqlDataReader lector = cmd.ExecuteReader();
                while (lector.Read())
                {
                    contador = lector.GetInt32(0);
                }
                lector.Close();
                conexion.Close();
            }catch (MySqlException ex){
                Console.WriteLine(ex.Message);
            }finally{
            conexion.Dispose();
            }
            return contador;
        }
        public static Documento[] obtenerInformacion(int total){
            Documento[] documentos;
            if (total > 0)
            {
                 documentos = new Documento[total];

                int i = 0;
                MySqlConnection conexion = conectar();
                conexion.Open();
                //Revisa que no este enviado (enviado=0) y haya pasado todas las aprobaciones
                //Para agilizar busqueda se buscan los ultimos 30 dias
                
                String query = "SELECT s.soc_nom,DATE_FORMAT(TIMBRE.added_at,'%Y-%m-%d'),'KR'," + //DATE_FORMAT(TIMBRE.added_at,'%Y-%m-%d')
                        " DATE_FORMAT(TIMBRE.added_at,'%m'), COMPROBANTE.MONEDA,CONCAT('F.',COMPROBANTE.FOLIO)," +
                        "trans.noAcreedor,CAST(CAST(COMPROBANTE.TOTAL AS DECIMAL(12,4)) AS CHAR(50))," +
                        "i.indicador,cc.cuenta,CAST(CAST(COMPROBANTE.SUBTOTAL AS DECIMAL(12,4)) AS CHAR(50)),cc.CeCo," +
                        "CONCAT(CONCAT('F.',CONCAT(COMPROBANTE.SERIE,COMPROBANTE.FOLIO)),CONCAT(' ',SUBSTRING(EMISOR.NOMBRE,1,30)))" +
                        ",i.cuenta_mayor,CAST(COMPROBANTE.TOTAL AS DECIMAL(12,4))- CAST(COMPROBANTE.SUBTOTAL AS DECIMAL(12,4))," +
                        "TIMBRE.UUID,factcc.fa_fkey, factcc.preliminar,SUBSTRING(EMISOR.NOMBRE,1,30),COMPROBANTE.SERIE " +
                        "FROM CFDI_COMPROBANTE COMPROBANTE " +
                        "INNER JOIN CFDI_COMPLEMENTO COMPLEMENTO ON COMPROBANTE.CFDI_COMPROBANTE_PKEY = COMPLEMENTO.CFDI_COMPROBANTE_FKEY " +
                        "INNER JOIN TFD_TIMBREFISCALDIGITAL TIMBRE ON TIMBRE.CFDI_COMPLEMENTO_FKEY = COMPLEMENTO.CFDI_COMPLEMENTO_PKEY " +
                        "INNER JOIN CFDI_EMISOR EMISOR ON EMISOR.CFDI_COMPROBANTE_FKEY = COMPROBANTE.CFDI_COMPROBANTE_PKEY " +
                        "INNER JOIN fact_autorizada f ON f.fact_fkey = TIMBRE.UUID " +
                        "INNER JOIN factaprobada_cuentacontable factcc ON factcc.fa_fkey = f.factauto_pkey " +
                        "INNER JOIN transferencia trans ON trans.id = factcc.trans_fkey " +
                        "INNER JOIN cuenta_contable cc ON factcc.cc_fkey = cc.cc_pkey " +
                        "INNER JOIN superuserfact suf ON suf.factauto_fkey = f.factauto_pkey " +
                        "INNER JOIN iva_sap i ON i.iva_pkey = factcc.iva_fkey " +
                        "INNER JOIN sociedades s ON s.id_soc = cc.soc_fkey " +
                        "WHERE factcc.acepto=1 AND factcc.aprobo=1 AND f.user_auto = 1 AND f.chief_auto=1 " +
                        "AND factcc.enviado=0 AND suf.APROBO = 1 AND ((suf.super_user_fkey IS NULL) OR (suf.APROBO2=1)) " +
                        "AND STR_TO_DATE(TIMBRE.FECHATIMBRADO,'%Y-%m-%d') > DATE_SUB(CURDATE(),INTERVAL 40 DAY)" +
                        "GROUP BY TIMBRE.UUID";
                //COMPROBANTE.FOLIO
                //AND COMPROBANTE.FOLIO IS NOT NULL 
                //Empieza a leer parametros necesarios
                try
                {
                    MySqlCommand cmd = new MySqlCommand(query, conexion);
                    MySqlDataReader lector = cmd.ExecuteReader();
                    while (lector.Read())
                    {
                        String moneda = "MXN";
                        String referencia = "";
                        String descripcion = "";
                        if (!(lector.IsDBNull(4)))
                        {
                            moneda = lector.GetString(4).ToLower();
                            if (moneda.Contains("usd") || moneda.Contains("dlls") || moneda.Contains("lares")
                                || moneda.Contains("americano"))
                            {
                                moneda = "USD";
                            }
                            else
                            {
                                moneda = "MXN";
                            }
                        }
                        //Dependiendo de cuales campos se agregaron en el XML son los que pone en el encabezado
                        if (lector.IsDBNull(5) && lector.IsDBNull(19))
                        {
                            referencia = "F.";
                            descripcion = "F."+lector.GetString(18);
                        }
                        else if (!(lector.IsDBNull(5)) && lector.IsDBNull(19))
                        {
                            referencia = lector.GetString(5);
                            descripcion = lector.GetString(5)+" "+ lector.GetString(18);
                        }
                        else if (lector.IsDBNull(5) && !(lector.IsDBNull(19)))
                        {
                            referencia = "F."+lector.GetString(19);
                            descripcion = "F."+lector.GetString(19) + " " + lector.GetString(18);
                        }
                        else
                        {
                            referencia = lector.GetString(5);
                            descripcion = lector.GetString(12);
                        }
                        documentos[i] = new Documento(lector.GetString(0), lector.GetString(1), lector.GetString(2), lector.GetString(3),
                            moneda, referencia, lector.GetString(6), lector.GetString(7), lector.GetString(8),
                            lector.GetString(9), lector.GetString(10), lector.GetString(11), descripcion, lector.GetString(13),
                            lector.GetString(14), lector.GetString(15), lector.GetInt32(16), lector.GetInt32(17));
                        i++;
                    }
                    lector.Close();
                    conexion.Close();
                }
                catch (MySqlException ex)
                {
                    Console.WriteLine(ex.Message);
                }
                finally
                {
                    conexion.Dispose();
                }
            }
            else
            {
                documentos = new Documento[0];
            }
            return documentos;
        }
        static void Main(string[] args)
        {
            Console.WriteLine("Iniciando proceso de documentos contables");
            Console.WriteLine("Buscando...");
            
            //Crea arreglo de documentos pendinetes
            Documento[] documentos= obtenerInformacion(totalPendientes());
            
            //Si existen documentos pendientes ejecuta
            if(documentos.Length >= 1 && documentos[0] != null){
                Console.WriteLine("Se encontraron "+documentos.Length+ " registros pendientes.");
                Console.WriteLine("Conectandose a SAP..");
                
            /* Codigo de pruebas
             * ambiente: PRO*/
                servicio_sap_pro.ZFM_REG_DOC_FIClient consulta = new servicio_sap_pro.ZFM_REG_DOC_FIClient("ZFM_REG_DOC_FI");

                consulta.Open();
                ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(delegate { return true; });
                if (consulta.State == CommunicationState.Opened)
                {
                    
                    foreach (Documento documento in documentos)
                    {
                        
                            if (documento != null)
                            {
                                //Asigna valores de acuerdo al mapeo en SAP
                                var esPreliminar = "";
                                servicio_sap_pro.ZstDocCont docContable = new servicio_sap_pro.ZstDocCont();
                                docContable.CompCode = documento.noSociedad;
                                docContable.DocDate = documento.fechaContabilizacion;
                                docContable.PstngDate = documento.fechaRecepcion;
                                docContable.DocType = documento.tipoDocumento;
                                docContable.Monat = documento.mesContabilizacion;
                                docContable.Currency = documento.tipoMoneda;
                                docContable.RefDocNo = documento.identificadorDocumento;
                                docContable.HeaderTxt = documento.encabezadoDocumento;
                                docContable.VendorNo = documento.noAcreedor;
                                docContable.ImpTotal = Decimal.Parse(documento.total);
                                docContable.TaxCode = documento.indicadorIva;
                                docContable.GlAccount = documento.noCuentaContable;
                                docContable.ImpSubtotal = Decimal.Parse(documento.subtotal);
                                docContable.Costcenter = documento.centroCostos;
                                docContable.TxtPos = documento.descripcionDocumento;
                                docContable.TaxAccount = documento.cuentaMayor;
                                docContable.ImpTax = Decimal.Parse(documento.totalImpuestos);
                                docContable.HeaderTxt = documento.encabezadoDocumento;
                                docContable.Uuid = documento.uuid;
                                /*Valor de preliminar es por omision 0
                                 * si el usuario en el portal selecciono preliminar se marca 1
                                 * */
                                if (documento.preliminar == 1)
                                {
                                    esPreliminar = "X";
                                }

                                //Inicia comunicación a SAP
                                try
                                {
                                    Console.WriteLine("Creando documento...");

                                    servicio_sap_pro.Bapireturn1 respuesta = consulta.ZfmRegDocFi(docContable, esPreliminar, "");
                                    
                                    //Dependiendo de la respuesta almacena el No.Documento o el tipo de error
                                    if (respuesta.Message.ToLower().Contains("el documento se ha contabilizado correctamente") && esPreliminar == "")
                                    {
                                        documento.enviado = 1;
                                        documento.error = "";
                                        documento.noDocumento = int.Parse(respuesta.Message.Substring(respuesta.Message.IndexOf("BKPFF") + 6, 10));
                                        Console.WriteLine("Exitosamente creado documento No. : " + documento.noDocumento);
                                    }
                                    else if (esPreliminar != "")
                                    {
                                        documento.enviado = 1;
                                        documento.error = "";
                                        documento.noDocumento = int.Parse(respuesta.Message);
                                        Console.WriteLine("Exitosamente creado documento No. : " + documento.noDocumento);
                                    }
                                    else
                                    {
                                        documento.enviado = 2;
                                        documento.noDocumento = 0;
                                        documento.error = respuesta.Message.ToLower();
                                        Console.WriteLine("Error en SAP: " + documento.error);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine("Error en conexion SAP: " + ex.Message);
                                }
                            }
                    }

                    Console.WriteLine("Proceso finalizado.");
                }
                        
            actualizaRegistro(documentos);
            }else{
                Console.WriteLine("No hay nuevos registros.");
            }
            
        }
        
    }
}
