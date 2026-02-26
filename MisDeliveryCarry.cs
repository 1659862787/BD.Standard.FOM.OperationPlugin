using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Orm.Metadata.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingdee.BOS.Core.Metadata.FieldElement;
using BD.Standard.FOM.Program.Utils;

namespace BD.Standard.OFM.OperationPlugin
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("其他出库单携带申请人，地址")]
    public class MisDeliveryCarry: AbstractOperationServicePlugIn
    {

        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            List<Field> file = this.BusinessInfo.GetFieldList();
            foreach (Field item in file)
            {
                e.FieldKeys.Add(item.Key);
            }
        }


        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            base.EndOperationTransaction(e);
            try
            {
                foreach (DynamicObject entity in e.DataEntitys)
                {
                    //获取当前表单fid,实体
                    string fid = entity[0].ToString();

                    DynamicObjectCollection entrys = entity["BillEntry"] as DynamicObjectCollection;

                    DynamicObjectCollection FEntity_Link = entrys[0]["FEntity_Link"] as DynamicObjectCollection;

                    
                    if (FEntity_Link.Count>0 && FEntity_Link[0]["STableName"].ToString().Equals("T_STK_OUTSTOCKAPPLYENTRY"))
                    {
                        string SBillid = FEntity_Link[0]["SBillid"].ToString();
                        DynamicObjectCollection dys = DBUtils.ExecuteDynamicObject(this.Context, $"select F_RPNA_Applicant,F_RPNA_Address from T_STK_OUTSTOCKAPPLY where fid={SBillid}");
                        if (dys.Count > 0 && dys[0]["F_RPNA_Applicant"]!=null && !string.IsNullOrWhiteSpace(dys[0]["F_RPNA_Applicant"].ToString()))
                        {
                            DBUtils.Execute(this.Context, $"update T_STK_MISDELIVERY set F_RPNA_Applicant='{dys[0]["F_RPNA_Applicant"]}',F_RPNA_Address='{dys[0]["F_RPNA_Address"]}' where fid={fid} ");
                        }
                        
                    }

                }

            }
            catch (Exception ex)
            {
                throw new KDException("保存二开插件异常信息：", ex.Message);
            }

        }



    }
}
